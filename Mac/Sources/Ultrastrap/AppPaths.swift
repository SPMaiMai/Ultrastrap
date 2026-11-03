import Foundation

enum AppPaths {
    static let appSupportName = "Ultrastrap"

    static var applicationSupport: URL {
        let base = FileManager.default.urls(for: .applicationSupportDirectory, in: .userDomainMask).first
            ?? URL(fileURLWithPath: NSHomeDirectory()).appendingPathComponent("Library/Application Support")
        return base.appendingPathComponent(appSupportName, isDirectory: true)
    }

    static var settingsFile: URL { applicationSupport.appendingPathComponent("Settings.json") }
    static var accountsFile: URL { applicationSupport.appendingPathComponent("Accounts.json") }
    static var favoritesFile: URL { applicationSupport.appendingPathComponent("Favorites.json") }
    static var flagsFile: URL { applicationSupport.appendingPathComponent("ClientSettings/ClientAppSettings.json") }
    static var logFile: URL { applicationSupport.appendingPathComponent("Ultrastrap.log") }

    static func ensureDirectories() {
        let fm = FileManager.default
        try? fm.createDirectory(at: applicationSupport, withIntermediateDirectories: true)
        try? fm.createDirectory(
            at: applicationSupport.appendingPathComponent("ClientSettings", isDirectory: true),
            withIntermediateDirectories: true
        )
    }
}
