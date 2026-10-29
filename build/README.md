# Building EasyJob releases

Needs the .NET 10 SDK (and internet on the first run, to download the runtime packs for self-contained builds).
.NET cross-compiles every target, so any one machine can build all of them.

```sh
build/build.sh                          # macOS, Linux, Git Bash, WSL
```
```powershell
.\build\build.ps1                       # Windows PowerShell / pwsh
```

| Option (sh / ps1) | Meaning |
| --- | --- |
| `--rids "win-x64 osx-arm64"` / `-Rids win-x64,osx-arm64` | Only these platforms. Default: `win-x64 win-arm64 linux-x64 linux-arm64 osx-x64 osx-arm64` |
| `--variants portable` / `-Variants portable` | Only one variant: `portable` or `framework`. Default: both |
| `--version 2.1.0` / `-Version 2.1.0` | Version for the file names and the app. Default: `<Version>` in `EasyJob.csproj` |
| `--clean` / `-Clean` | Delete `build/out` and `build/dist` first |

## What you get (`build/dist/`)

| File | Contents |
| --- | --- |
| `EasyJob-<version>-<rid>-portable.zip` | Self-contained: the .NET runtime is included, nothing to install. Bigger (about 45-50 MB) |
| `EasyJob-<version>-<rid>.zip` | Framework-dependent: small (about 12-16 MB), needs the **.NET 10 runtime** (`Microsoft.NETCore.App`) installed |

Windows and Linux zips unpack to an `EasyJob/` folder (program, `config.json`, sample scripts, `EasyJobPSTools`, license,
`policy.example.json`). macOS zips contain `EasyJob.app`. The publish folders stay in `build/out/` for inspection.

The in-app updater picks the zip that matches the running OS and CPU, and the same variant as the installed copy
(a portable install updates to the portable zip). Upload all the zips to one GitHub release.

## Notes

* macOS: the bundle is ad-hoc signed only when built on a Mac. A downloaded, unsigned app may need
  `xattr -dr com.apple.quarantine EasyJob.app` (or right-click, Open) the first time.
* On a Mac the `.app` zip is made with `ditto`, which keeps the code signatures stored in extended attributes; unzip it with Finder (double-click) or `ditto -x -k`, not with the plain `unzip` command.
* The Windows script sets Unix permissions inside the zips, so macOS/Linux builds made on Windows still start.
* Linux needs a desktop session with X11 or Wayland and the usual fontconfig/libX11 libraries (present on normal desktops).
