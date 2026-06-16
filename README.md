# LockCat

![LockCat pixel logo](src/LockPig/Assets/Pixel/target-logo.png)

Hi, I am LockCat. I am a small pixel cat that sits on your Windows desktop and helps you temporarily lock your keyboard and screen. Most of the time I stay quiet. When you need a tiny guard on duty, I put on my serious face.

![LockCat desktop cat](src/LockPig/Assets/Cat/Sprites/cat-idle.png)

[Chinese guide](README.zh-CN.md) | [Japanese guide](README.ja.md) | [Report an issue](https://github.com/Throb7777/LockCat/issues/new/choose)

## What LockCat Does

- Locks your keyboard with a hotkey.
- Optionally turns off the screen when locking.
- Restores control with another hotkey.
- Lives in the system tray for quick access.
- Shows a small animated pixel cat on the desktop.
- Remembers your settings between launches.
- Keeps install and uninstall files inside its own `LockCat` folder.

## Requirements

- Windows 10 or later.
- x64 Windows build.
- No account or network connection is required for normal use.

## Download and Install

1. Download `LockCatInstaller.exe` from [GitHub Releases](https://github.com/Throb7777/LockCat/releases).
2. Run the installer.
3. Pick an install location if you want to change the default.
4. Choose whether LockCat should start with Windows.
5. Finish setup. I will sit in the tray and, if enabled, appear on your desktop.

When you choose a folder, I always create my own `LockCat` folder inside it:

```text
You choose: D:\Apps
I install to: D:\Apps\LockCat
```

That keeps my files in one tidy little home.

## Default Hotkeys

- Lock: `Ctrl + Alt + K`
- Restore: `Ctrl + Alt + P`

You can change both hotkeys in Settings.

## Desktop Cat

The desktop cat is not just decoration. It is the small front door to LockCat.

- Single-click me and I will react.
- Triple-click me to lock. The first two times, I will ask before locking so you know what is happening.
- Right-click me to open the quick menu.
- Drag me to move me around.
- Hide or show me from Settings or the tray menu.
- Keep me on top if you want me to stay visible above other windows.

## Settings

Open Settings from the tray icon or the cat quick menu.

You can change:

- Lock hotkey.
- Restore hotkey.
- Whether the screen turns off during lock.
- Whether the desktop cat is visible.
- Whether the cat stays on top.
- Startup behavior.
- Interface language.

## Uninstalling

Use the Start menu uninstall entry or run `LockCatUninstaller.exe` from the install folder.

Important little cat promise: the uninstaller only removes files that LockCat installed. It does not wipe the whole folder you selected during setup. If you keep settings, your hotkeys and preferences stay ready for the next install.

## Privacy

LockCat does not read the text you type. It listens for the configured hotkeys and uses simple activity signals for desktop cat animations. Your settings are stored locally on your computer.

## Build From Source

Install the .NET 8 SDK, then run:

```powershell
dotnet build src\LockPig\LockPig.csproj -c Release
dotnet build src\LockCat.Installer\LockCat.Installer.csproj -c Release
dotnet build src\LockCat.Uninstaller\LockCat.Uninstaller.csproj -c Release
dotnet run --project qa-uninstall-safety\LockCatUninstallSafetyProbe.csproj -c Release
```

The main app project is `src/LockPig`, and the installer/uninstaller projects are in `src/LockCat.Installer` and `src/LockCat.Uninstaller`.

## Feedback

If something feels odd, please open an issue:

https://github.com/Throb7777/LockCat/issues/new/choose

I will bring my tiny notebook and take a look.

## License

LockCat is released under the MIT License. See [LICENSE](LICENSE) and [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).
