import AppKit
import SwiftUI

struct AccountsView: View {
    @EnvironmentObject private var state: AppState
    @State private var cookie = ""
    @State private var alias = ""
    @State private var selectedId: Int64?

    private var selected: AccountRecord? {
        state.accounts.accounts.first { $0.userId == selectedId }
    }

    var body: some View {
        VStack(alignment: .leading, spacing: 16) {
            Text("Accounts")
                .font(.largeTitle.weight(.semibold))
            Text("Cookies are stored in the macOS Keychain for this user only. Never share a .ROBLOSECURITY cookie.")
                .foregroundStyle(Brand.secondaryText)

            SectionCard(title: "Add account") {
                SecureField(".ROBLOSECURITY cookie", text: $cookie)
                    .textFieldStyle(.roundedBorder)
                TextField("Alias (optional)", text: $alias)
                    .textFieldStyle(.roundedBorder)
                Button("Validate and save") {
                    Task { await addAccount() }
                }
                .buttonStyle(.borderedProminent)
                .disabled(cookie.trimmingCharacters(in: .whitespacesAndNewlines).isEmpty || state.isBusy)
            }

            SectionCard(
                title: "Saved accounts",
                subtitle: state.settings.activeAccountAlias.isEmpty
                    ? "Active account: (none)"
                    : "Active account: \(state.settings.activeAccountAlias)"
            ) {
                if state.accounts.accounts.isEmpty {
                    Text("No accounts yet.")
                        .foregroundStyle(Brand.secondaryText)
                } else {
                    List(selection: $selectedId) {
                        ForEach(state.accounts.accounts) { account in
                            VStack(alignment: .leading) {
                                Text(account.alias)
                                Text("\(account.username) · \(account.userId)")
                                    .font(.caption)
                                    .foregroundStyle(Brand.secondaryText)
                            }
                            .tag(Optional(account.userId))
                        }
                    }
                    .frame(minHeight: 160)
                    .listStyle(.inset)
                    HStack {
                        Button("Set active") {
                            if let selected {
                                state.settings.activeAccountAlias = selected.alias
                                state.saveSettings()
                                state.statusMessage = "Active account is \(selected.alias)."
                            }
                        }
                        Button("Refresh") {
                            Task { await refreshSelected() }
                        }
                        Button("Copy ticket") {
                            Task { await copyTicket() }
                        }
                        Spacer()
                        Button("Remove", role: .destructive) {
                            if let selected {
                                if state.settings.activeAccountAlias == selected.alias {
                                    state.settings.activeAccountAlias = ""
                                    state.saveSettings()
                                }
                                state.accounts.remove(selected)
                                selectedId = nil
                                state.statusMessage = "Account removed."
                            }
                        }
                    }
                    .disabled(selected == nil || state.isBusy)
                }
            }

            StatusBar(text: state.statusMessage, busy: state.isBusy)
            Spacer()
        }
    }

    private func addAccount() async {
        state.isBusy = true
        defer { state.isBusy = false }
        state.statusMessage = "Validating cookie with Roblox..."
        do {
            let account = try await state.accounts.addOrUpdate(cookie: cookie, alias: alias)
            cookie = ""
            alias = ""
            selectedId = account.userId
            state.statusMessage = "Added account '\(account.alias)' (\(account.username))."
        } catch {
            state.statusMessage = error.localizedDescription
        }
    }

    private func refreshSelected() async {
        guard let selected else { return }
        state.isBusy = true
        defer { state.isBusy = false }
        do {
            let updated = try await state.accounts.refresh(selected)
            selectedId = updated.userId
            state.statusMessage = "Refreshed \(updated.alias)."
        } catch {
            state.statusMessage = error.localizedDescription
        }
    }

    private func copyTicket() async {
        guard let selected else { return }
        state.isBusy = true
        defer { state.isBusy = false }
        if let ticket = await state.accounts.ticket(for: selected) {
            NSPasteboard.general.clearContents()
            NSPasteboard.general.setString(ticket, forType: .string)
            state.statusMessage = "Copied a fresh authentication ticket."
        } else {
            state.statusMessage = "Could not generate a ticket for that account."
        }
    }
}
