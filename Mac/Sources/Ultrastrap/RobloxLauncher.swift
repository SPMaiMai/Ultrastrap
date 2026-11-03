import Foundation

enum RobloxLauncherError: LocalizedError {
    case playerMissing
    case studioMissing
    case startFailed(String)

    var errorDescription: String? {
        switch self {
        case .playerMissing:
            return "Roblox is not installed. Install it from roblox.com/download, then try again."
        case .studioMissing:
            return "Roblox Studio is not installed."
        case .startFailed(let message):
            return message
        }
    }
}

enum RobloxLauncher {
    static func parsePlaceId(_ input: String?) -> Int64? {
        guard let text = input?.trimmingCharacters(in: .whitespacesAndNewlines), !text.isEmpty else {
            return nil
        }
        if let value = Int64(text), value > 0 {
            return value
        }
        if let regex = try? NSRegularExpression(pattern: #"roblox\.com/games/(\d+)"#, options: .caseInsensitive) {
            let range = NSRange(text.startIndex..<text.endIndex, in: text)
            if let match = regex.firstMatch(in: text, range: range),
               let idRange = Range(match.range(at: 1), in: text),
               let value = Int64(text[idRange]), value > 0 {
                return value
            }
        }
        if let url = URL(string: text),
           let items = URLComponents(url: url, resolvingAgainstBaseURL: false)?.queryItems {
            for key in ["placeId", "placeid"] {
                if let value = items.first(where: { $0.name.caseInsensitiveCompare(key) == .orderedSame })?.value,
                   let id = Int64(value), id > 0 {
                    return id
                }
            }
        }
        return nil
    }

    static func buildPlayerLaunchArgs(placeId: Int64, ticket: String?, jobId: String?, accessCode: String?) -> String {
        let tracker = Int64.random(in: 10_000_000_000...9_000_000_000_000)
        let request: String
        if let accessCode, !accessCode.isEmpty {
            request = "RequestPrivateGame"
        } else if let jobId, !jobId.isEmpty {
            request = "RequestGameJob"
        } else {
            request = "RequestGame"
        }

        var query = "request=\(request)&browserTrackerId=\(tracker)&placeId=\(placeId)&isPlayTogetherGame=false"
        if let jobId, !jobId.isEmpty {
            query += "&gameId=\(urlEncode(jobId))"
        }
        if let accessCode, !accessCode.isEmpty {
            query += "&accessCode=\(urlEncode(accessCode))"
        }

        let launcherURL = "https://assetgame.roblox.com/game/PlaceLauncher.ashx?\(query)"
        let launchTime = Int64(Date().timeIntervalSince1970 * 1000)
        return "roblox-player:1+launchmode:play"
            + "+gameinfo:\(ticket ?? "")"
            + "+launchtime:\(launchTime)"
            + "+placelauncherurl:\(urlEncode(launcherURL))"
            + "+browsertrackerid:\(tracker)"
            + "+robloxLocale:en_us+gameLocale:en_us+channel:+LaunchExp:InApp"
    }

    @MainActor
    static func launchStudio() throws {
        guard let app = RobloxInstall.studioApp else { throw RobloxLauncherError.studioMissing }
        try runOpen(arguments: ["-a", app.path])
        AppLog.line("RobloxLauncher", "Launched Studio at \(app.path)")
    }

    @MainActor
    static func launchPlayerHome(multiInstance: Bool, flags: FastFlagService, applyFlags: Bool) throws {
        try prepareLaunch(multiInstance: multiInstance, flags: flags, applyFlags: applyFlags)
        guard let app = RobloxInstall.playerApp else { throw RobloxLauncherError.playerMissing }
        if multiInstance, let binary = RobloxInstall.playerBinary {
            try spawnBinary(binary, arguments: [])
        } else {
            try runOpen(arguments: ["-a", app.path, "roblox-player:"])
        }
        AppLog.line("RobloxLauncher", "Launched Roblox home")
    }

    @MainActor
    static func startPlayer(launchArgs: String, multiInstance: Bool, flags: FastFlagService, applyFlags: Bool = true) throws {
        try prepareLaunch(multiInstance: multiInstance, flags: flags, applyFlags: applyFlags)
        guard let binary = RobloxInstall.playerBinary, let app = RobloxInstall.playerApp else {
            throw RobloxLauncherError.playerMissing
        }

        if multiInstance {
            try spawnBinary(binary, arguments: [launchArgs])
        } else {
            try runOpen(arguments: ["-a", app.path, launchArgs])
        }
        AppLog.line("RobloxLauncher", "Started player (\(multiInstance ? "direct" : "open"))")
    }

    @MainActor
    static func launchMany(
        requests: [LaunchRequest],
        settings: AppSettings,
        accounts: AccountService,
        flags: FastFlagService,
        applyFlags: Bool,
        progress: @escaping (String) -> Void
    ) async -> Int {
        guard !requests.isEmpty else { return 0 }
        guard RobloxInstall.isPlayerInstalled else {
            progress("Roblox is not installed yet. Install it from roblox.com/download first.")
            return 0
        }

        let needsMulti = settings.multiInstanceLaunching || requests.count > 1 || InstanceService.anyRobloxRunning()
        let stagger = max(250, min(settings.launchStaggerMs, 10_000))
        var started = 0

        for (index, request) in requests.enumerated() {
            let label = request.windowLabel
                ?? request.account?.alias
                ?? request.account?.username
                ?? "window \(index + 1)"
            progress("Launching \(label) (\(index + 1)/\(requests.count))…")

            var ticket: String?
            if let account = request.account {
                ticket = await accounts.ticket(for: account)
            }
            let args = buildPlayerLaunchArgs(
                placeId: request.placeId,
                ticket: ticket,
                jobId: request.jobId,
                accessCode: request.accessCode
            )
            do {
                try startPlayer(launchArgs: args, multiInstance: needsMulti, flags: flags, applyFlags: applyFlags)
                started += 1
            } catch {
                AppLog.error("RobloxLauncher.launchMany", error)
                progress(error.localizedDescription)
            }

            if index < requests.count - 1 {
                try? await Task.sleep(nanoseconds: UInt64(stagger) * 1_000_000)
            }
        }

        progress(started == 0 ? "No Roblox windows were started." : "Started \(started) Roblox window(s).")
        return started
    }

    @MainActor
    private static func prepareLaunch(multiInstance: Bool, flags: FastFlagService, applyFlags: Bool) throws {
        guard RobloxInstall.isPlayerInstalled else { throw RobloxLauncherError.playerMissing }
        if applyFlags, !flags.flags.isEmpty {
            _ = flags.applyToRoblox()
        }
        if multiInstance {
            clearKnownLockFiles()
        }
    }

    private static func spawnBinary(_ binary: URL, arguments: [String]) throws {
        let process = Process()
        process.executableURL = binary
        process.arguments = arguments
        process.currentDirectoryURL = binary.deletingLastPathComponent()
        process.standardOutput = FileHandle.nullDevice
        process.standardError = FileHandle.nullDevice
        try process.run()
    }

    private static func runOpen(arguments: [String]) throws {
        let process = Process()
        process.executableURL = URL(fileURLWithPath: "/usr/bin/open")
        process.arguments = arguments
        try process.run()
        process.waitUntilExit()
        if process.terminationStatus != 0 {
            throw RobloxLauncherError.startFailed("open failed with status \(process.terminationStatus)")
        }
    }

    private static func clearKnownLockFiles() {
        let candidates = [
            NSTemporaryDirectory() + "RobloxPlayer.lock",
            NSTemporaryDirectory() + "roblox.lock",
            NSHomeDirectory() + "/Library/Application Support/Roblox/SingletonMutex"
        ]
        for path in candidates where FileManager.default.fileExists(atPath: path) {
            try? FileManager.default.removeItem(atPath: path)
            AppLog.line("RobloxLauncher", "Removed lock file \(path)")
        }
    }

    private static func urlEncode(_ value: String) -> String {
        var allowed = CharacterSet.urlQueryAllowed
        allowed.remove(charactersIn: ":/?#[]@!$&'()*+,;=")
        return value.addingPercentEncoding(withAllowedCharacters: allowed) ?? value
    }
}
