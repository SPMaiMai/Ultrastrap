import Foundation

struct AccountRecord: Codable, Identifiable, Hashable {
    var alias: String
    var userId: Int64
    var username: String
    var displayName: String

    var id: Int64 { userId }

    var displayLabel: String {
        username.isEmpty || username == alias ? alias : "\(alias) (\(username))"
    }
}

struct AccountsFile: Codable {
    var accounts: [AccountRecord] = []
}

struct FavoritePlace: Codable, Identifiable, Hashable {
    var name: String
    var placeId: Int64
    var jobId: String?
    var accessCode: String?
    var lastUsed: Date = Date()

    var id: String { "\(placeId)-\(name)" }

    var displayLabel: String {
        var extra = ""
        if let accessCode, !accessCode.isEmpty {
            extra = " (private)"
        } else if let jobId, !jobId.isEmpty {
            extra = " (specific server)"
        }
        return "\(name)  ·  \(placeId)\(extra)"
    }
}

struct FavoritesFile: Codable {
    var places: [FavoritePlace] = []
}

struct AppSettings: Codable, Equatable {
    var multiInstanceLaunching: Bool = true
    var autoTileWindows: Bool = false
    var renameWindowsByAccount: Bool = true
    var launchStaggerMs: Int = 1500
    var activeAccountAlias: String = ""
    var lastPlaceId: String = ""
    var useFastFlagManager: Bool = true
    var confirmLaunches: Bool = false
    var copies: Int = 1
}

struct RobloxInstanceInfo: Identifiable, Hashable {
    var processId: pid_t
    var name: String
    var bundleIdentifier: String
    var isStudio: Bool
    var launchDate: Date?

    var id: pid_t { processId }

    var kindLabel: String { isStudio ? "Studio" : "Player" }
}

struct LaunchRequest {
    var placeId: Int64
    var account: AccountRecord?
    var jobId: String?
    var accessCode: String?
    var windowLabel: String?
}
