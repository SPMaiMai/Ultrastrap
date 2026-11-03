import AppKit
import Combine
import Foundation
import SwiftUI

enum SidebarPage: String, CaseIterable, Identifiable, Hashable {
    case launch
    case quickLaunch
    case accounts
    case instances
    case fastFlags
    case settings
    case about

    var id: String { rawValue }

    var title: String {
        switch self {
        case .launch: return "Launch"
        case .quickLaunch: return "Quick Launch"
        case .accounts: return "Accounts"
        case .instances: return "Instances"
        case .fastFlags: return "Fast Flags"
        case .settings: return "Settings"
        case .about: return "About"
        }
    }

    var systemImage: String {
        switch self {
        case .launch: return "play.rectangle.fill"
        case .quickLaunch: return "rectangle.on.rectangle.angled"
        case .accounts: return "person.2.fill"
        case .instances: return "square.grid.2x2.fill"
        case .fastFlags: return "flag.fill"
        case .settings: return "gearshape.fill"
        case .about: return "info.circle.fill"
        }
    }
}

@MainActor
final class AppState: ObservableObject {
    @Published var settings: AppSettings
    @Published var favorites: [FavoritePlace]
    @Published var page: SidebarPage = .launch
    @Published var statusMessage = "Ready."
    @Published var isBusy = false
    @Published var robloxInstalled = RobloxInstall.isPlayerInstalled
    @Published var studioInstalled = RobloxInstall.isStudioInstalled

    let accounts = AccountService()
    let flags = FastFlagService()
    let instances = InstanceService()
    private var cancellables = Set<AnyCancellable>()

    init() {
        AppPaths.ensureDirectories()
        settings = JsonStore.load(AppSettings.self, from: AppPaths.settingsFile, fallback: AppSettings())
        favorites = JsonStore.load(FavoritesFile.self, from: AppPaths.favoritesFile, fallback: FavoritesFile()).places
        instances.refresh()
        accounts.objectWillChange.sink { [weak self] _ in self?.objectWillChange.send() }.store(in: &cancellables)
        flags.objectWillChange.sink { [weak self] _ in self?.objectWillChange.send() }.store(in: &cancellables)
        instances.objectWillChange.sink { [weak self] _ in self?.objectWillChange.send() }.store(in: &cancellables)
    }

    func saveSettings() {
        JsonStore.save(settings, to: AppPaths.settingsFile)
    }

    func saveFavorites() {
        JsonStore.save(FavoritesFile(places: favorites), to: AppPaths.favoritesFile)
    }

    var activeAccount: AccountRecord? {
        if settings.activeAccountAlias.isEmpty { return nil }
        return accounts.account(alias: settings.activeAccountAlias)
    }

    func refreshInstallState() {
        robloxInstalled = RobloxInstall.isPlayerInstalled
        studioInstalled = RobloxInstall.isStudioInstalled
        instances.refresh()
    }

    func launchRoblox() {
        Task { await performLaunchRoblox() }
    }

    func launchStudio() {
        do {
            try RobloxLauncher.launchStudio()
            statusMessage = "Launched Roblox Studio."
        } catch {
            statusMessage = error.localizedDescription
        }
    }

    func launchPlace(copies: Int, allAccounts: Bool) async {
        guard let placeId = RobloxLauncher.parsePlaceId(settings.lastPlaceId) else {
            statusMessage = "Enter a place ID or a roblox.com/games/... URL."
            return
        }
        saveSettings()

        let clamped = min(max(copies, 1), 16)
        var requests: [LaunchRequest] = []
        if allAccounts {
            if accounts.accounts.isEmpty {
                statusMessage = "Save at least one account first."
                return
            }
            requests = accounts.accounts.map {
                LaunchRequest(placeId: placeId, account: $0, jobId: nil, accessCode: nil, windowLabel: $0.alias)
            }
        } else {
            let account = activeAccount
            requests = (1...clamped).map { index in
                LaunchRequest(
                    placeId: placeId,
                    account: account,
                    jobId: nil,
                    accessCode: nil,
                    windowLabel: clamped == 1 ? account?.alias : "\(account?.alias ?? "Roblox") #\(index)"
                )
            }
        }

        await runLaunches(requests)
    }

    func launchRequests(_ requests: [LaunchRequest]) async {
        await runLaunches(requests)
    }

    private func performLaunchRoblox() async {
        if settings.confirmLaunches, InstanceService.anyRobloxRunning(), !settings.multiInstanceLaunching {
            statusMessage = "Roblox is already running. Enable multiple windows or close it first."
            return
        }
        isBusy = true
        defer { isBusy = false }
        do {
            try RobloxLauncher.launchPlayerHome(
                multiInstance: settings.multiInstanceLaunching,
                flags: flags,
                applyFlags: settings.useFastFlagManager
            )
            statusMessage = "Launched Roblox."
        } catch {
            statusMessage = error.localizedDescription
        }
        refreshInstallState()
    }

    private func runLaunches(_ requests: [LaunchRequest]) async {
        isBusy = true
        defer { isBusy = false }
        if requests.count > 1 {
            settings.multiInstanceLaunching = true
            saveSettings()
        }
        let started = await RobloxLauncher.launchMany(
            requests: requests,
            settings: settings,
            accounts: accounts,
            flags: flags,
            applyFlags: settings.useFastFlagManager,
            progress: { [weak self] message in
                self?.statusMessage = message
            }
        )
        if started > 0, settings.autoTileWindows {
            try? await Task.sleep(nanoseconds: 4_000_000_000)
            WindowTiler.tileRunningRoblox(mode: .grid)
        }
        refreshInstallState()
    }
}
