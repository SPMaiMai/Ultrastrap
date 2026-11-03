import SwiftUI

struct QuickLaunchView: View {
    @EnvironmentObject private var state: AppState
    @State private var jobId = ""
    @State private var accessCode = ""
    @State private var selectedAccount: AccountRecord?
    @State private var copies: Double = 1
    @State private var favoriteName = ""

    var body: some View {
        ScrollView {
            VStack(alignment: .leading, spacing: 16) {
                Text("Quick Launch")
                    .font(.largeTitle.weight(.semibold))
                Text("Launch a place as one account, several copies, or every saved account.")
                    .foregroundStyle(Brand.secondaryText)

                SectionCard(title: "Place", subtitle: "Place ID or roblox.com/games/... URL") {
                    TextField("123456789 or https://www.roblox.com/games/...", text: $state.settings.lastPlaceId)
                        .textFieldStyle(.roundedBorder)
                        .onChange(of: state.settings.lastPlaceId) { _ in
                            state.saveSettings()
                        }
                    HStack {
                        TextField("Job ID (optional)", text: $jobId)
                            .textFieldStyle(.roundedBorder)
                        TextField("Private server code (optional)", text: $accessCode)
                            .textFieldStyle(.roundedBorder)
                    }
                }

                SectionCard(title: "Account") {
                    Picker("Launch as", selection: $selectedAccount) {
                        Text("Default / none").tag(Optional<AccountRecord>.none)
                        ForEach(state.accounts.accounts) { account in
                            Text(account.displayLabel).tag(Optional(account))
                        }
                    }
                    Toggle("Allow multiple Roblox windows", isOn: $state.settings.multiInstanceLaunching)
                        .onChange(of: state.settings.multiInstanceLaunching) { _ in
                            state.saveSettings()
                        }
                    HStack {
                        Text("Copies")
                        Slider(value: $copies, in: 1...16, step: 1)
                        Text("\(Int(copies))")
                            .frame(width: 28)
                    }
                }

                HStack(spacing: 10) {
                    Button("Launch 1 window") {
                        Task { await launch(copies: 1, allAccounts: false) }
                    }
                    .buttonStyle(.borderedProminent)
                    Button("Launch copies") {
                        Task { await launch(copies: Int(copies), allAccounts: false) }
                    }
                    Button("Launch all accounts") {
                        Task { await launch(copies: 1, allAccounts: true) }
                    }
                    .disabled(state.accounts.accounts.isEmpty)
                }
                .disabled(state.isBusy)

                SectionCard(title: "Favorites") {
                    HStack {
                        TextField("Favorite name", text: $favoriteName)
                            .textFieldStyle(.roundedBorder)
                        Button("Save current place") { saveFavorite() }
                            .disabled(RobloxLauncher.parsePlaceId(state.settings.lastPlaceId) == nil || favoriteName.trimmingCharacters(in: .whitespaces).isEmpty)
                    }
                    if state.favorites.isEmpty {
                        Text("No favorites yet.")
                            .foregroundStyle(Brand.secondaryText)
                    } else {
                        ForEach(state.favorites) { favorite in
                            HStack {
                                VStack(alignment: .leading) {
                                    Text(favorite.name)
                                    Text(favorite.displayLabel)
                                        .font(.caption)
                                        .foregroundStyle(Brand.secondaryText)
                                }
                                Spacer()
                                Button("Use") {
                                    state.settings.lastPlaceId = String(favorite.placeId)
                                    jobId = favorite.jobId ?? ""
                                    accessCode = favorite.accessCode ?? ""
                                    state.saveSettings()
                                }
                                Button("Remove", role: .destructive) {
                                    state.favorites.removeAll { $0.id == favorite.id }
                                    state.saveFavorites()
                                }
                            }
                        }
                    }
                }

                StatusBar(text: state.statusMessage, busy: state.isBusy)
            }
        }
        .onAppear {
            copies = Double(max(1, min(state.settings.copies, 16)))
            if selectedAccount == nil {
                selectedAccount = state.activeAccount
            }
        }
    }

    private func launch(copies: Int, allAccounts: Bool) async {
        if let selectedAccount {
            state.settings.activeAccountAlias = selectedAccount.alias
            state.saveSettings()
        }
        state.settings.copies = copies
        state.saveSettings()

        guard let placeId = RobloxLauncher.parsePlaceId(state.settings.lastPlaceId) else {
            state.statusMessage = "Enter a place ID or a roblox.com/games/... URL."
            return
        }

        if allAccounts {
            let requests = state.accounts.accounts.map {
                LaunchRequest(placeId: placeId, account: $0, jobId: emptyToNil(jobId), accessCode: emptyToNil(accessCode), windowLabel: $0.alias)
            }
            await state.launchRequests(requests)
            return
        }

        let account = selectedAccount ?? state.activeAccount
        let requests = (1...copies).map { index in
            LaunchRequest(
                placeId: placeId,
                account: account,
                jobId: emptyToNil(jobId),
                accessCode: emptyToNil(accessCode),
                windowLabel: copies == 1 ? account?.alias : "\(account?.alias ?? "Roblox") #\(index)"
            )
        }
        await state.launchRequests(requests)
    }

    private func saveFavorite() {
        guard let placeId = RobloxLauncher.parsePlaceId(state.settings.lastPlaceId) else { return }
        let favorite = FavoritePlace(
            name: favoriteName.trimmingCharacters(in: .whitespacesAndNewlines),
            placeId: placeId,
            jobId: emptyToNil(jobId),
            accessCode: emptyToNil(accessCode)
        )
        state.favorites.removeAll { $0.placeId == placeId && $0.name == favorite.name }
        state.favorites.insert(favorite, at: 0)
        state.saveFavorites()
        favoriteName = ""
        state.statusMessage = "Saved favorite \(favorite.name)."
    }

    private func emptyToNil(_ value: String) -> String? {
        let trimmed = value.trimmingCharacters(in: .whitespacesAndNewlines)
        return trimmed.isEmpty ? nil : trimmed
    }
}
