import AppKit
import SwiftUI

enum Brand {
    static let name = "Ultrastrap"
    static let version = "1.0.0"
    static let owner = "Ultrastrap"
    static let repository = "SPMaiMai/Ultrastrap"
    static let githubURL = URL(string: "https://github.com/SPMaiMai/Ultrastrap")!
    static let releasesURL = URL(string: "https://github.com/SPMaiMai/Ultrastrap/releases")!
    static let issuesURL = URL(string: "https://github.com/SPMaiMai/Ultrastrap/issues/new")!
    static let robloxDownloadURL = URL(string: "https://www.roblox.com/download")!

    static let accent = Color(red: 0.39, green: 0.64, blue: 0.95)
    static let background = Color(red: 0.09, green: 0.10, blue: 0.12)
    static let card = Color(red: 0.145, green: 0.155, blue: 0.18)
    static let cardHover = Color(red: 0.18, green: 0.20, blue: 0.24)
    static let secondaryText = Color.white.opacity(0.55)

    static var icon: NSImage {
        if let image = loadPNG("Ultrastrap") {
            return image
        }
        return NSImage(systemSymbolName: "square.grid.2x2.fill", accessibilityDescription: name) ?? NSImage()
    }

    static var swiftUIIcon: Image {
        Image(nsImage: icon)
    }

    private static func loadPNG(_ name: String) -> NSImage? {
        var urls: [URL] = []
        if let url = Bundle.main.url(forResource: name, withExtension: "png") {
            urls.append(url)
        }
        #if SWIFT_PACKAGE
        if let url = Bundle.module.url(forResource: name, withExtension: "png") {
            urls.append(url)
        }
        #endif
        let exeDir = URL(fileURLWithPath: Bundle.main.bundlePath)
        urls.append(exeDir.appendingPathComponent("Contents/Resources/\(name).png"))
        urls.append(exeDir.appendingPathComponent("\(name).png"))

        for url in urls {
            if let image = NSImage(contentsOf: url), image.size.width > 0 {
                return image
            }
        }
        return nil
    }
}
