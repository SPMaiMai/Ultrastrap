import Foundation

enum JsonStore {
    private static let encoder: JSONEncoder = {
        let encoder = JSONEncoder()
        encoder.outputFormatting = [.prettyPrinted, .sortedKeys]
        encoder.dateEncodingStrategy = .iso8601
        return encoder
    }()

    private static let decoder: JSONDecoder = {
        let decoder = JSONDecoder()
        decoder.dateDecodingStrategy = .iso8601
        return decoder
    }()

    static func load<T: Decodable>(_ type: T.Type, from url: URL, fallback: T) -> T {
        AppPaths.ensureDirectories()
        guard FileManager.default.fileExists(atPath: url.path),
              let data = try? Data(contentsOf: url)
        else {
            return fallback
        }
        do {
            return try decoder.decode(T.self, from: data)
        } catch {
            AppLog.line("JsonStore", "Failed to decode \(url.lastPathComponent): \(error)")
            return fallback
        }
    }

    static func save<T: Encodable>(_ value: T, to url: URL) {
        AppPaths.ensureDirectories()
        do {
            let data = try encoder.encode(value)
            try data.write(to: url, options: .atomic)
        } catch {
            AppLog.line("JsonStore", "Failed to write \(url.lastPathComponent): \(error)")
        }
    }
}
