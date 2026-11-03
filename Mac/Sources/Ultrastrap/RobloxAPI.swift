import Foundation

enum RobloxAPIError: LocalizedError {
    case invalidCookie
    case missingCSRF
    case missingTicket
    case http(Int)

    var errorDescription: String? {
        switch self {
        case .invalidCookie:
            return "That cookie is invalid or expired."
        case .missingCSRF:
            return "Roblox did not return a CSRF token."
        case .missingTicket:
            return "Roblox did not return an authentication ticket."
        case .http(let code):
            return "Roblox API failed with HTTP \(code)."
        }
    }
}

enum RobloxAPI {
    private static let ticketURL = URL(string: "https://auth.roblox.com/v1/authentication-ticket/")!
    private static let userURL = URL(string: "https://users.roblox.com/v1/users/authenticated")!

    static func makeSession() -> URLSession {
        let config = URLSessionConfiguration.ephemeral
        config.httpShouldSetCookies = false
        config.httpCookieAcceptPolicy = .never
        config.timeoutIntervalForRequest = 15
        config.timeoutIntervalForResource = 20
        return URLSession(configuration: config)
    }

    static func request(url: URL, cookie: String, method: String = "GET", csrf: String? = nil) -> URLRequest {
        var request = URLRequest(url: url)
        request.httpMethod = method
        request.setValue("Roblox/WinInet", forHTTPHeaderField: "User-Agent")
        request.setValue(".ROBLOSECURITY=\(cookie)", forHTTPHeaderField: "Cookie")
        request.setValue("https://www.roblox.com/", forHTTPHeaderField: "Referer")
        request.setValue("https://www.roblox.com", forHTTPHeaderField: "Origin")
        if method == "POST" {
            request.setValue("application/json", forHTTPHeaderField: "Content-Type")
            request.httpBody = Data()
        }
        if let csrf, !csrf.isEmpty {
            request.setValue(csrf, forHTTPHeaderField: "X-CSRF-TOKEN")
        }
        return request
    }

    static func authenticatedUser(cookie: String) async throws -> (id: Int64, name: String, displayName: String) {
        let session = makeSession()
        let request = request(url: userURL, cookie: cookie)
        let (data, response) = try await session.data(for: request)
        let code = (response as? HTTPURLResponse)?.statusCode ?? 0
        guard code == 200 else { throw RobloxAPIError.invalidCookie }

        guard let json = try JSONSerialization.jsonObject(with: data) as? [String: Any],
              let idValue = json["id"],
              let name = json["name"] as? String
        else {
            throw RobloxAPIError.invalidCookie
        }

        let id: Int64
        if let number = idValue as? Int64 {
            id = number
        } else if let number = idValue as? Int {
            id = Int64(number)
        } else if let number = idValue as? NSNumber {
            id = number.int64Value
        } else {
            throw RobloxAPIError.invalidCookie
        }

        let displayName = (json["displayName"] as? String) ?? name
        return (id, name, displayName)
    }

    static func authenticationTicket(cookie: String) async throws -> String {
        let session = makeSession()
        let prime = request(url: ticketURL, cookie: cookie, method: "POST")
        let (_, primeResponse) = try await session.data(for: prime)
        let csrf = (primeResponse as? HTTPURLResponse)?.value(forHTTPHeaderField: "x-csrf-token") ?? ""
        if csrf.isEmpty {
            throw RobloxAPIError.missingCSRF
        }

        let ticketRequest = request(url: ticketURL, cookie: cookie, method: "POST", csrf: csrf)
        let (_, ticketResponse) = try await session.data(for: ticketRequest)
        guard let http = ticketResponse as? HTTPURLResponse else {
            throw RobloxAPIError.missingTicket
        }
        if let ticket = http.value(forHTTPHeaderField: "rbx-authentication-ticket"), !ticket.isEmpty {
            return ticket
        }
        throw RobloxAPIError.http(http.statusCode)
    }

    static func cleanCookie(_ raw: String) -> String {
        var cookie = raw.trimmingCharacters(in: .whitespacesAndNewlines)
        if let range = cookie.range(of: ".ROBLOSECURITY=", options: .caseInsensitive) {
            cookie = String(cookie[range.upperBound...])
            if let end = cookie.firstIndex(of: ";") {
                cookie = String(cookie[..<end])
            }
        }
        return cookie.trimmingCharacters(in: .whitespacesAndNewlines)
    }
}
