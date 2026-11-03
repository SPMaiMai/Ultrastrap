import AppKit
import SwiftUI

@main
struct UltrastrapApp: App {
    @NSApplicationDelegateAdaptor(AppDelegate.self) private var appDelegate
    @StateObject private var appState = AppState()

    var body: some Scene {
        Window("Ultrastrap", id: "main") {
            RootView()
                .environmentObject(appState)
                .frame(minWidth: 860, minHeight: 560)
                .background(Brand.background)
                .preferredColorScheme(.dark)
                .task {
                    await CLI.apply(CLI.parse(CommandLine.arguments), to: appState)
                }
        }
        .defaultSize(width: 960, height: 640)
        .commands {
            CommandGroup(replacing: .newItem) {}
            CommandGroup(after: .appInfo) {
                Button("Ultrastrap GitHub") {
                    NSWorkspace.shared.open(Brand.githubURL)
                }
            }
        }
    }
}

final class AppDelegate: NSObject, NSApplicationDelegate {
    func applicationDidFinishLaunching(_ notification: Notification) {
        NSApp.setActivationPolicy(.regular)
        NSApp.appearance = NSAppearance(named: .darkAqua)
        NSApp.applicationIconImage = Brand.icon
        NSApp.activate(ignoringOtherApps: true)
        AppLog.line("App", "Ultrastrap \(Brand.version) started")
    }

    func applicationShouldTerminateAfterLastWindowClosed(_ sender: NSApplication) -> Bool {
        true
    }
}
