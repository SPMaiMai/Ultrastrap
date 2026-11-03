import Foundation

@MainActor
final class AccountService: ObservableObject {
    @Published private(set) var accounts: [AccountRecord] = []

    init() {
        reload()
    }

    func reload() {
        accounts = JsonStore.load(AccountsFile.self, from: AppPaths.accountsFile, fallback: AccountsFile()).accounts
    }

    func save() {
        JsonStore.save(AccountsFile(accounts: accounts), to: AppPaths.accountsFile)
    }

    func cookie(for account: AccountRecord) -> String? {
        KeychainStore.loadCookie(userId: account.userId)
    }

    func addOrUpdate(cookie rawCookie: String, alias: String?) async throws -> AccountRecord {
        let cookie = RobloxAPI.cleanCookie(rawCookie)
        guard !cookie.isEmpty else { throw RobloxAPIError.invalidCookie }

        let info = try await RobloxAPI.authenticatedUser(cookie: cookie)
        let finalAlias = {
            if let alias, !alias.trimmingCharacters(in: .whitespacesAndNewlines).isEmpty {
                return alias.trimmingCharacters(in: .whitespacesAndNewlines)
            }
            return info.name
        }()

        var record = AccountRecord(
            alias: finalAlias,
            userId: info.id,
            username: info.name,
            displayName: info.displayName
        )

        if let index = accounts.firstIndex(where: { $0.userId == info.id }) {
            record.alias = finalAlias
            accounts[index] = record
        } else {
            accounts.append(record)
        }

        KeychainStore.saveCookie(userId: info.id, cookie: cookie)
        save()
        AppLog.line("AccountService", "Saved account '\(record.alias)' (\(record.userId))")
        return record
    }

    func remove(_ account: AccountRecord) {
        accounts.removeAll { $0.userId == account.userId }
        KeychainStore.deleteCookie(userId: account.userId)
        save()
    }

    func account(alias: String) -> AccountRecord? {
        accounts.first { $0.alias.compare(alias, options: .caseInsensitive) == .orderedSame }
    }

    func refresh(_ account: AccountRecord) async throws -> AccountRecord {
        guard let cookie = cookie(for: account) else { throw RobloxAPIError.invalidCookie }
        return try await addOrUpdate(cookie: cookie, alias: account.alias)
    }

    func ticket(for account: AccountRecord) async -> String? {
        guard let cookie = cookie(for: account) else {
            AppLog.line("AccountService", "No cookie in Keychain for \(account.alias)")
            return nil
        }
        do {
            return try await RobloxAPI.authenticationTicket(cookie: cookie)
        } catch {
            AppLog.error("AccountService.ticket", error)
            return nil
        }
    }
}
