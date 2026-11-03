import AppKit
import SwiftUI

struct LaunchMenuView: View {
    @EnvironmentObject private var state: AppState

    var body: some View {
        VStack(alignment: .leading, spacing: 18) {
            HStack(spacing: 14) {
                Brand.swiftUIIcon
                    .resizable()
                    .interpolation(.high)
                    .frame(width: 64, height: 64)
                VStack(alignment: .leading, spacing: 4) {
                    Text("Ultrastrap")
                        .font(.largeTitle.weight(.semibold))
                    Text("Mac launcher for multiple Roblox windows, accounts, and Fast Flags.")
                        .foregroundStyle(Brand.secondaryText)
                }
            }

            VStack(spacing: 10) {
                CardButton(
                    title: "Launch Roblox",
                    subtitle: state.robloxInstalled ? "Open the Roblox player" : "Install Roblox first",
                    systemImage: "play.fill",
                    enabled: !state.isBusy,
                    action: {
                        if state.robloxInstalled {
                            state.launchRoblox()
                        } else {
                            NSWorkspace.shared.open(Brand.robloxDownloadURL)
                            state.statusMessage = "Opened the Roblox download page."
                        }
                    }
                )
                CardButton(
                    title: "Launch Roblox Studio",
                    subtitle: state.studioInstalled ? "Open Studio" : "Studio is not installed",
                    systemImage: "hammer.fill",
                    enabled: state.studioInstalled && !state.isBusy,
                    action: state.launchStudio
                )
                CardButton(
                    title: "Multi-window launch",
                    subtitle: "Open several Roblox windows, same or different accounts",
                    systemImage: "rectangle.on.rectangle.angled",
                    action: { state.page = .quickLaunch }
                )
                CardButton(
                    title: "Account manager",
                    subtitle: state.accounts.accounts.isEmpty
                        ? "Save .ROBLOSECURITY cookies in the Keychain"
                        : "\(state.accounts.accounts.count) saved account(s)",
                    systemImage: "person.2.fill",
                    action: { state.page = .accounts }
                )
                CardButton(
                    title: "Running instances",
                    subtitle: "Focus, tile, or close Roblox windows",
                    systemImage: "square.grid.2x2.fill",
                    action: { state.page = .instances }
                )
                CardButton(
                    title: "Configure settings",
                    subtitle: "Behaviour, Fast Flags, and launch options",
                    systemImage: "gearshape.fill",
                    action: { state.page = .settings }
                )
            }

            Spacer()
            StatusBar(text: state.statusMessage, busy: state.isBusy)
        }
    }
}
