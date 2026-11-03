import AppKit
import Foundation

@MainActor
final class InstanceService: ObservableObject {
    @Published private(set) var instances: [RobloxInstanceInfo] = []

    func refresh() {
        instances = Self.runningInstances()
    }

    static func runningInstances() -> [RobloxInstanceInfo] {
        NSWorkspace.shared.runningApplications.compactMap { app in
            guard let info = classify(app) else { return nil }
            return info
        }
        .sorted { ($0.launchDate ?? .distantPast) < ($1.launchDate ?? .distantPast) }
    }

    static func anyRobloxRunning() -> Bool {
        !runningInstances().isEmpty
    }

    func terminate(_ instance: RobloxInstanceInfo) {
        guard let app = NSRunningApplication(processIdentifier: instance.processId) else { return }
        if !app.terminate() {
            app.forceTerminate()
        }
        DispatchQueue.main.asyncAfter(deadline: .now() + 0.4) { [weak self] in
            self?.refresh()
        }
    }

    func terminateAll() {
        for instance in instances {
            if let app = NSRunningApplication(processIdentifier: instance.processId) {
                app.forceTerminate()
            }
        }
        DispatchQueue.main.asyncAfter(deadline: .now() + 0.4) { [weak self] in
            self?.refresh()
        }
    }

    func focus(_ instance: RobloxInstanceInfo) {
        NSRunningApplication(processIdentifier: instance.processId)?
            .activate(options: [.activateIgnoringOtherApps])
    }

    private static func classify(_ app: NSRunningApplication) -> RobloxInstanceInfo? {
        let bundle = (app.bundleIdentifier ?? "").lowercased()
        let name = (app.localizedName ?? "").lowercased()
        let exe = (app.executableURL?.lastPathComponent ?? "").lowercased()
        let combined = "\(bundle) \(name) \(exe)"

        guard combined.contains("roblox") else { return nil }
        if bundle.contains("ultrastrap") || name.contains("ultrastrap") {
            return nil
        }
        if exe.contains("installer") || name.contains("installer") {
            return nil
        }

        let isStudio = combined.contains("studio")
        return RobloxInstanceInfo(
            processId: app.processIdentifier,
            name: app.localizedName ?? (isStudio ? "Roblox Studio" : "Roblox"),
            bundleIdentifier: app.bundleIdentifier ?? "",
            isStudio: isStudio,
            launchDate: app.launchDate
        )
    }
}
