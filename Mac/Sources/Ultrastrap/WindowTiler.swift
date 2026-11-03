import AppKit
import ApplicationServices
import Foundation

enum TileMode: String, CaseIterable, Identifiable {
    case grid
    case horizontal
    case vertical

    var id: String { rawValue }
    var title: String {
        switch self {
        case .grid: return "Grid"
        case .horizontal: return "Horizontal"
        case .vertical: return "Vertical"
        }
    }
}

enum WindowTiler {
    static func requestAccess() -> Bool {
        let options = [kAXTrustedCheckOptionPrompt.takeUnretainedValue() as String: true] as CFDictionary
        return AXIsProcessTrustedWithOptions(options)
    }

    static func tileRunningRoblox(mode: TileMode) {
        guard requestAccess() else {
            AppLog.line("WindowTiler", "Accessibility permission is required to tile windows")
            return
        }

        let apps = NSWorkspace.shared.runningApplications.filter { app in
            let blob = "\(app.bundleIdentifier ?? "") \(app.localizedName ?? "") \(app.executableURL?.lastPathComponent ?? "")".lowercased()
            return blob.contains("roblox") && !blob.contains("ultrastrap") && !blob.contains("installer")
        }
        tile(apps: apps, mode: mode)
    }

    static func tile(apps: [NSRunningApplication], mode: TileMode) {
        guard !apps.isEmpty, let screen = NSScreen.main else { return }
        let visible = screen.visibleFrame
        let count = apps.count
        let columns: Int
        let rows: Int
        switch mode {
        case .horizontal:
            columns = count
            rows = 1
        case .vertical:
            columns = 1
            rows = count
        case .grid:
            columns = Int(ceil(sqrt(Double(count))))
            rows = Int(ceil(Double(count) / Double(columns)))
        }

        let width = visible.width / CGFloat(columns)
        let height = visible.height / CGFloat(rows)

        for (index, app) in apps.enumerated() {
            app.activate(options: [.activateIgnoringOtherApps])
            let column = index % columns
            let row = index / columns
            let cocoaX = visible.minX + CGFloat(column) * width
            let cocoaY = visible.maxY - CGFloat(row + 1) * height
            setFrame(pid: app.processIdentifier, x: cocoaX, y: cocoaY, width: width, height: height, screen: screen)
        }
    }

    private static func setFrame(pid: pid_t, x: CGFloat, y: CGFloat, width: CGFloat, height: CGFloat, screen: NSScreen) {
        let appElement = AXUIElementCreateApplication(pid)
        var windowsRef: AnyObject?
        let result = AXUIElementCopyAttributeValue(appElement, kAXWindowsAttribute as CFString, &windowsRef)
        guard result == .success, let windows = windowsRef as? [AXUIElement], let window = windows.first else {
            return
        }

        let axY = (screen.frame.maxY - (y + height))
        var origin = CGPoint(x: x, y: axY)
        var size = CGSize(width: width, height: height)
        if let position = AXValueCreate(.cgPoint, &origin) {
            AXUIElementSetAttributeValue(window, kAXPositionAttribute as CFString, position)
        }
        if let axSize = AXValueCreate(.cgSize, &size) {
            AXUIElementSetAttributeValue(window, kAXSizeAttribute as CFString, axSize)
        }
    }
}
