import Foundation

enum CLI {
    struct Options {
        var page: SidebarPage?
        var place: String?
        var copies: Int?
        var account: String?
        var allAccounts = false
        var jobId: String?
        var accessCode: String?
        var launchStudio = false
        var launchPlayer = false
    }

    static func parse(_ arguments: [String]) -> Options {
        var options = Options()
        var index = 0
        let args = Array(arguments.dropFirst())
        while index < args.count {
            let arg = args[index]
            switch arg.lowercased() {
            case "--accounts", "-accounts":
                options.page = .accounts
            case "--launch", "-launch", "--multilaunch", "-multilaunch":
                options.page = .quickLaunch
            case "--instances", "-instances":
                options.page = .instances
            case "--settings", "-menu", "--menu":
                options.page = .settings
            case "--studio", "-studio":
                options.launchStudio = true
            case "--player", "-player":
                options.launchPlayer = true
            case "--allaccounts", "-allaccounts":
                options.allAccounts = true
            case "--place", "-place":
                options.place = nextValue(args, &index)
            case "--copies", "-copies":
                if let value = nextValue(args, &index), let number = Int(value) {
                    options.copies = number
                }
            case "--account", "-account":
                options.account = nextValue(args, &index)
            case "--job", "-job":
                options.jobId = nextValue(args, &index)
            case "--accesscode", "-accesscode":
                options.accessCode = nextValue(args, &index)
            default:
                if arg.hasPrefix("roblox-player:") || arg.hasPrefix("roblox:") {
                    options.place = arg
                    options.launchPlayer = true
                }
            }
            index += 1
        }
        return options
    }

    @MainActor
    static func apply(_ options: Options, to state: AppState) async {
        if let page = options.page {
            state.page = page
        }
        if let place = options.place {
            state.settings.lastPlaceId = place
            state.saveSettings()
        }
        if let alias = options.account {
            state.settings.activeAccountAlias = alias
            state.saveSettings()
        }
        if options.launchStudio {
            state.launchStudio()
            return
        }
        if let place = options.place,
           place.lowercased().hasPrefix("roblox-player:") || place.lowercased().hasPrefix("roblox:") {
            do {
                try RobloxLauncher.startPlayer(
                    launchArgs: place,
                    multiInstance: state.settings.multiInstanceLaunching,
                    flags: state.flags,
                    applyFlags: state.settings.useFastFlagManager
                )
                state.statusMessage = "Launched Roblox from URI."
            } catch {
                state.statusMessage = error.localizedDescription
            }
            return
        }
        if let placeId = RobloxLauncher.parsePlaceId(options.place) {
            let copies = min(max(options.copies ?? 1, 1), 16)
            let job = options.jobId
            let access = options.accessCode
            if options.allAccounts {
                let requests = state.accounts.accounts.map {
                    LaunchRequest(placeId: placeId, account: $0, jobId: job, accessCode: access, windowLabel: $0.alias)
                }
                await state.launchRequests(requests)
            } else {
                let account = state.activeAccount
                let requests = (1...copies).map { index in
                    LaunchRequest(
                        placeId: placeId,
                        account: account,
                        jobId: job,
                        accessCode: access,
                        windowLabel: copies == 1 ? account?.alias : "\(account?.alias ?? "Roblox") #\(index)"
                    )
                }
                await state.launchRequests(requests)
            }
            return
        }
        if options.launchPlayer {
            state.launchRoblox()
        }
    }

    private static func nextValue(_ args: [String], _ index: inout Int) -> String? {
        guard index + 1 < args.count else { return nil }
        index += 1
        return args[index]
    }
}
