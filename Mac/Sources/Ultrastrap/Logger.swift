import Foundation

enum AppLog {
    static func line(_ ident: String, _ message: String) {
        let stamp = ISO8601DateFormatter().string(from: Date())
        let text = "[\(stamp)] [\(ident)] \(message)\n"
        fputs(text, stderr)
        appendToFile(text)
    }

    static func error(_ ident: String, _ error: Error) {
        line(ident, "\(error)")
    }

    private static func appendToFile(_ text: String) {
        let url = AppPaths.logFile
        AppPaths.ensureDirectories()
        if !FileManager.default.fileExists(atPath: url.path) {
            FileManager.default.createFile(atPath: url.path, contents: nil)
        }
        guard let handle = try? FileHandle(forWritingTo: url) else { return }
        defer { try? handle.close() }
        handle.seekToEndOfFile()
        if let data = text.data(using: .utf8) {
            handle.write(data)
        }
    }
}
