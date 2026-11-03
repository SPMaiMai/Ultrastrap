import Foundation

@MainActor
final class FastFlagService: ObservableObject {
    static let presetMap: [String: String] = [
        "Rendering.ManualFullscreen": "FFlagHandleAltEnterFullscreenManually",
        "Rendering.DisableScaling": "DFFlagDisableDPIScale",
        "Rendering.MSAA": "FIntDebugForceMSAASamples",
        "Rendering.TextureQuality.OverrideEnabled": "DFFlagTextureQualityOverrideEnabled",
        "Rendering.TextureQuality.Level": "DFIntTextureQualityOverride",
        "Rendering.TargetFps": "DFIntTaskSchedulerTargetFps"
    ]

    @Published var flags: [String: String] = [:]
    @Published var applyError: String?

    init() {
        reload()
    }

    func reload() {
        flags = Self.readDictionary(from: AppPaths.flagsFile)
    }

    func save() {
        AppPaths.ensureDirectories()
        do {
            let data = try JSONSerialization.data(withJSONObject: flags, options: [.prettyPrinted, .sortedKeys])
            try data.write(to: AppPaths.flagsFile, options: .atomic)
        } catch {
            AppLog.error("FastFlagService.save", error)
        }
    }

    func set(_ key: String, _ value: String?) {
        if let value, !value.isEmpty {
            flags[key] = value
        } else {
            flags.removeValue(forKey: key)
        }
    }

    func setPreset(_ name: String, _ value: String?) {
        guard let key = Self.presetMap[name] else { return }
        set(key, value)
    }

    func getPreset(_ name: String) -> String? {
        guard let key = Self.presetMap[name] else { return nil }
        return flags[key]
    }

    func applyToRoblox() -> String? {
        save()
        applyError = nil

        guard RobloxInstall.isPlayerInstalled else {
            let message = "Roblox is not installed, so Fast Flags were saved but not applied."
            applyError = message
            return message
        }

        var errors: [String] = []
        if let dir = RobloxInstall.playerClientSettingsDirectory,
           let file = RobloxInstall.playerClientSettingsFile {
            do {
                try FileManager.default.createDirectory(at: dir, withIntermediateDirectories: true)
                let data = try JSONSerialization.data(withJSONObject: flags, options: [.prettyPrinted, .sortedKeys])
                try data.write(to: file, options: .atomic)
                AppLog.line("FastFlagService", "Wrote flags to \(file.path)")
            } catch {
                errors.append("Could not write \(file.path): \(error.localizedDescription)")
            }
        }

        let userFile = RobloxInstall.userFastFlagFile
        do {
            try FileManager.default.createDirectory(at: userFile.deletingLastPathComponent(), withIntermediateDirectories: true)
            let data = try JSONSerialization.data(withJSONObject: flags, options: [.prettyPrinted, .sortedKeys])
            try data.write(to: userFile, options: .atomic)
        } catch {
            AppLog.line("FastFlagService", "Optional user flag file failed: \(error.localizedDescription)")
        }

        if errors.isEmpty {
            return nil
        }
        let message = errors.joined(separator: "\n")
        applyError = message
        return message
    }

    private static func readDictionary(from url: URL) -> [String: String] {
        guard FileManager.default.fileExists(atPath: url.path),
              let data = try? Data(contentsOf: url),
              let object = try? JSONSerialization.jsonObject(with: data)
        else {
            return [:]
        }

        if let strings = object as? [String: String] {
            return strings
        }
        if let mixed = object as? [String: Any] {
            var result: [String: String] = [:]
            for (key, value) in mixed {
                result[key] = String(describing: value)
            }
            return result
        }
        return [:]
    }
}
