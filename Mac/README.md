# Ultrastrap for Mac

Native **macOS** Ultrastrap. This is a new SwiftUI app, not a port of the Windows WPF project — that Windows build cannot run on a Mac.

Requires **macOS 13 Ventura** or later, plus **Xcode** (or the Command Line Tools) so `swift` is available.

## What it does

- Launch Roblox and Roblox Studio from the installed `.app` bundles
- Multi-window launch by starting `RobloxPlayer` directly (macOS has no `ROBLOX_singletonMutex`)
- Place ID / `roblox.com/games/...` launch with optional job ID or private-server code
- Account manager: `.ROBLOSECURITY` cookies live in the **Keychain**, not in a JSON file
- Fresh authentication tickets, same Roblox API flow as the Windows app
- Fast Flags, re-applied into `Roblox.app` on every launch
- Instance list: focus, tile, or close running Player/Studio processes

Roblox on Mac is `/Applications/Roblox.app`. Ultrastrap does not replace that installer; it launches the copy you already have and writes Fast Flags beside the player binary.

## Build on your Mac

```bash
cd Mac
chmod +x build-mac.sh
./build-mac.sh
open dist/Ultrastrap.app
```

Or from Xcode: **File → Open** `Mac/Package.swift`, select the Ultrastrap scheme, and Run.

The first unsigned launch may be blocked by Gatekeeper. Right-click **Ultrastrap.app → Open**, or:

```bash
xattr -dr com.apple.quarantine dist/Ultrastrap.app
```

## Data locations

| What | Where |
| --- | --- |
| Settings, accounts list, favorites, logs | `~/Library/Application Support/Ultrastrap/` |
| Account cookies | Keychain service `com.spmaimai.ultrastrap` |
| Fast Flags (Ultrastrap copy) | `~/Library/Application Support/Ultrastrap/ClientSettings/ClientAppSettings.json` |
| Fast Flags applied at launch | `/Applications/Roblox.app/Contents/MacOS/ClientSettings/ClientAppSettings.json` |

## Notes

- Same-account duplicate sessions can still be kicked by Roblox. Use different saved accounts when you need two characters in one experience.
- Tiling windows asks for Accessibility permission (System Settings → Privacy & Security → Accessibility).
- Fast Flags inside `Roblox.app` are wiped when Roblox updates; Ultrastrap writes them again the next time you launch.
- This Mac app is version **1.0.0**, same as the Windows Ultrastrap line.

Do not share `.ROBLOSECURITY` cookies. They are equivalent to your password.
