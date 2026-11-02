# Ultrastrap

**Ultrastrap** is a feature-expanded fork of [Bloxstrap](https://github.com/bloxstraplabs/bloxstrap), the third-party replacement for the standard Roblox bootstrapper on Windows.

It keeps everything Bloxstrap does (fast flags, mods, custom bootstrappers, Discord rich presence, activity tracking, etc.) and adds a set of power-user features on top.

> This is an independent fork. It is not affiliated with or endorsed by Bloxstrap Labs or Roblox Corporation. All credit for the underlying bootstrapper goes to the Bloxstrap authors.

> Ultrastrap installs side-by-side with Bloxstrap (its own install folder, registry entries, and `Ultrastrap.exe`), so you can use both.

## What's new in Ultrastrap

### Multiple Roblox windows at once
Roblox normally only lets you run one client at a time. Ultrastrap holds the Roblox single-instance mutexes (`ROBLOX_singletonMutex` and `ROBLOX_singletonEvent`) open via a lightweight background watcher process, so you can run **as many Roblox windows as you like simultaneously** — the same account in several servers, or different accounts side-by-side.

- Enable it in **Settings → Behaviour** or **Quick Launch**.
- Multi-window launches turn it on automatically.
- The watcher self-singletons. Optionally keep it running after the last client closes.
- When enabled, the usual "Roblox is already running" confirmation is skipped.

Roblox may still disconnect a **duplicate session of the same account** from the same experience. Use different saved accounts when you need two characters in one game.

### Quick Launch
Open **Settings → Quick Launch** (also on the start menu and tray):

- Launch a place by ID or `roblox.com/games/...` URL
- Optional specific-server job ID or private-server access code
- **Launch 1 window** as a chosen account
- **Launch copies** — several windows of the same account
- **Launch all saved accounts** into the same place
- Favorite places for one-click reuse
- Auto-tile new windows and label them with the account name

### Built-in account manager
Add your Roblox accounts once and launch games as any of them.

- Stored by their `.ROBLOSECURITY` cookie, **encrypted with Windows DPAPI for your user only** (never written in plaintext).
- Ultrastrap validates the cookie against the Roblox API and remembers the username/display name.
- Pick an **active account**; when you launch a game, Ultrastrap generates a fresh authentication ticket for that account and injects it into the launch.
- Combined with multi-instancing, you can have several **different accounts** playing at the same time.
- Manage accounts in **Settings → Accounts**, or open it from the tray / start menu.

### Instance dashboard
**Settings → Instances** lists every running Roblox Player and Studio process:

- Focus, close one, or close all
- Tile windows in a grid, horizontally, or vertically
- Keep the mutex watcher running between sessions

### Quality-of-life tray actions
The notification-area menu gains:

- **Close all Roblox instances**
- **Account manager**
- **Multi-window launch**
- **Running instances**
- **Tile Roblox windows**

## Command-line additions
| Flag | Description |
| --- | --- |
| `-multiinstancewatcher` | Internal: runs the background process that holds the Roblox singleton mutexes open. |
| `-accounts` | Opens the account manager. |
| `-launch` / `-multilaunch` | Opens Quick Launch. |
| `-instances` | Opens the instance dashboard. |
| `-place <id-or-url>` | Launches that place (optionally with the flags below). |
| `-copies <n>` | Opens `n` windows of the selected/active account (1–16). |
| `-account "<alias>"` | Uses the stored account with that alias (fresh auth ticket). |
| `-allaccounts` | Launches every saved account into the place. |
| `-job <jobId>` | Joins a specific server. |
| `-accesscode <code>` | Joins a private server. |

Examples:

```
Ultrastrap.exe -place 123456789 -copies 3
Ultrastrap.exe -place https://www.roblox.com/games/123456789/Name -account "alt" 
Ultrastrap.exe -place 123456789 -allaccounts
Ultrastrap.exe "roblox-player:1+..." -account "alt"
```

The Roblox URI, if present, must come first.

## Building
Ultrastrap targets `net6.0-windows` (WPF + WinForms). With the .NET 6 SDK (or newer) installed:

```sh
dotnet build Bloxstrap.sln -c Release
```

The output binary is `Ultrastrap.exe`. To make a shareable installer (single file):

```sh
dotnet publish Bloxstrap\Bloxstrap.csproj -p:PublishSingleFile=true -r win-x64 -c Release --self-contained false
```

## Security note
Your `.ROBLOSECURITY` cookie is equivalent to your password. Ultrastrap encrypts stored cookies with DPAPI so they can only be read by your Windows user account on this machine. Only add accounts you own, and never share your cookies or generated authentication tickets.

## Credits
- [Bloxstrap](https://github.com/bloxstraplabs/bloxstrap) and its contributors for the original bootstrapper this fork is built on.
