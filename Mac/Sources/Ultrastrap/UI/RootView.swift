import SwiftUI

struct RootView: View {
    @EnvironmentObject private var state: AppState

    var body: some View {
        NavigationSplitView {
            VStack(alignment: .leading, spacing: 16) {
                HStack(spacing: 10) {
                    Brand.swiftUIIcon
                        .resizable()
                        .interpolation(.high)
                        .frame(width: 36, height: 36)
                    VStack(alignment: .leading, spacing: 1) {
                        Text(Brand.name)
                            .font(.title3.weight(.semibold))
                        Text("v\(Brand.version)")
                            .font(.caption)
                            .foregroundStyle(Brand.secondaryText)
                    }
                }
                .padding(.horizontal, 8)
                .padding(.top, 8)

                List(selection: $state.page) {
                    ForEach(SidebarPage.allCases) { page in
                        Label(page.title, systemImage: page.systemImage)
                            .tag(page)
                    }
                }
                .listStyle(.sidebar)

                VStack(alignment: .leading, spacing: 6) {
                    Label(state.robloxInstalled ? "Roblox found" : "Roblox not installed",
                          systemImage: state.robloxInstalled ? "checkmark.circle.fill" : "exclamationmark.triangle.fill")
                        .foregroundStyle(state.robloxInstalled ? Color.green : Color.orange)
                    Label(state.studioInstalled ? "Studio found" : "Studio not installed",
                          systemImage: state.studioInstalled ? "checkmark.circle.fill" : "minus.circle")
                        .foregroundStyle(state.studioInstalled ? Color.green : Brand.secondaryText)
                }
                .font(.caption)
                .padding(12)
            }
            .navigationSplitViewColumnWidth(min: 210, ideal: 230, max: 280)
            .background(Brand.background)
        } detail: {
            Group {
                switch state.page {
                case .launch: LaunchMenuView()
                case .quickLaunch: QuickLaunchView()
                case .accounts: AccountsView()
                case .instances: InstancesView()
                case .fastFlags: FastFlagsView()
                case .settings: SettingsView()
                case .about: AboutView()
                }
            }
            .frame(maxWidth: .infinity, maxHeight: .infinity)
            .background(Brand.background)
            .padding(20)
        }
        .navigationSplitViewStyle(.balanced)
        .tint(Brand.accent)
        .onAppear {
            state.refreshInstallState()
        }
    }
}
