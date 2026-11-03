import Foundation

enum RobloxInstall {
    static let playerBundleCandidates = [
        "/Applications/Roblox.app",
        "\(NSHomeDirectory())/Applications/Roblox.app"
    ]

    static let studioBundleCandidates = [
        "/Applications/RobloxStudio.app",
        "\(NSHomeDirectory())/Applications/RobloxStudio.app"
    ]

    static var playerApp: URL? {
        firstExisting(playerBundleCandidates)
    }

    static var studioApp: URL? {
        firstExisting(studioBundleCandidates)
    }

    static var playerBinary: URL? {
        guard let app = playerApp else { return nil }
        return firstExisting([
            app.appendingPathComponent("Contents/MacOS/RobloxPlayer").path,
            app.appendingPathComponent("Contents/MacOS/Roblox").path
        ])
    }

    static var studioBinary: URL? {
        guard let app = studioApp else { return nil }
        return firstExisting([
            app.appendingPathComponent("Contents/MacOS/RobloxStudio").path,
            app.appendingPathComponent("Contents/MacOS/RobloxStudioBeta").path
        ])
    }

    static var isPlayerInstalled: Bool { playerApp != nil && playerBinary != nil }
    static var isStudioInstalled: Bool { studioApp != nil }

    static var playerClientSettingsDirectory: URL? {
        guard let app = playerApp else { return nil }
        return app.appendingPathComponent("Contents/MacOS/ClientSettings", isDirectory: true)
    }

    static var playerClientSettingsFile: URL? {
        playerClientSettingsDirectory?.appendingPathComponent("ClientAppSettings.json")
    }

    static var userFastFlagFile: URL {
        URL(fileURLWithPath: NSHomeDirectory())
            .appendingPathComponent("Library/Preferences/com.roblox.Roblox/ClientAppSettings.json")
    }

    private static func firstExisting(_ paths: [String]) -> URL? {
        let fm = FileManager.default
        for path in paths where fm.fileExists(atPath: path) {
            return URL(fileURLWithPath: path)
        }
        return nil
    }
}
