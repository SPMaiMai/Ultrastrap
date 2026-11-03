import AppKit
import SwiftUI

struct SettingsView: View {
    @EnvironmentObject private var state: AppState

    var body: some View {
        ScrollView {
            VStack(alignment: .leading, spacing: 16) {
                Text("Settings")
                    .font(.largeTitle.weight(.semibold))

                SectionCard(title: "Behaviour") {
                    Toggle("Allow multiple Roblox windows", isOn: $state.settings.multiInstanceLaunching)
                    Toggle("Automatically tile new windows", isOn: $state.settings.autoTileWindows)
                    Toggle("Confirm when Roblox is already running", isOn: $state.settings.confirmLaunches)
                    HStack {
                        Text("Launch stagger")
                        Slider(
                            value: Binding(
                                get: { Double(state.settings.launchStaggerMs) },
                                set: { state.settings.launchStaggerMs = Int($0) }
                            ),
                            in: 250...5000,
                            step: 50
                        )
                        Text("\(state.settings.launchStaggerMs) ms")
                            .frame(width: 70, alignment: .trailing)
                    }
                    Text("Tickets are single-use, so extra windows are started a short delay apart.")
                        .font(.caption)
                        .foregroundStyle(Brand.secondaryText)
                }
                .onChange(of: state.settings) { _ in
                    state.saveSettings()
                }

                SectionCard(title: "Roblox") {
                    LabeledContent("Player") {
                        Text(RobloxInstall.playerApp?.path ?? "Not found")
                            .textSelection(.enabled)
                    }
                    LabeledContent("Player binary") {
                        Text(RobloxInstall.playerBinary?.path ?? "Not found")
                            .textSelection(.enabled)
                    }
                    LabeledContent("Studio") {
                        Text(RobloxInstall.studioApp?.path ?? "Not found")
                            .textSelection(.enabled)
                    }
                    Button("Open Roblox download page") {
                        NSWorkspace.shared.open(Brand.robloxDownloadURL)
                    }
                    Button("Reveal Ultrastrap data folder") {
                        NSWorkspace.shared.open(AppPaths.applicationSupport)
                    }
                }

                StatusBar(text: "Settings save automatically.")
            }
        }
    }
}
