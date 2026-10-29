# <img src="assets/icon.png" height="42" width="42"/> EasyJob

<a href="https://img.shields.io/github/license/akshinmustafayev/EasyJob">
  <img src="https://img.shields.io/github/license/akshinmustafayev/EasyJob" alt="License" />
</a>
<a href="https://img.shields.io/tokei/lines/github/akshinmustafayev/EasyJob">
  <img src="https://img.shields.io/tokei/lines/github/akshinmustafayev/EasyJob" alt="Total lines" />
</a>
<a href="https://img.shields.io/github/downloads/akshinmustafayev/EasyJob/total">
  <img src="https://img.shields.io/github/downloads/akshinmustafayev/EasyJob/total" alt="Downloads" />
</a>
<a href="https://img.shields.io/github/stars/akshinmustafayev/EasyJob?style=social">
  <img alt="GitHub repo file count" src="https://img.shields.io/github/stars/akshinmustafayev/EasyJob?style=social">
</a>
<a href="https://img.shields.io/github/contributors/akshinmustafayev/EasyJob">
  <img alt="GitHub repo file count" src="https://img.shields.io/github/contributors/akshinmustafayev/EasyJob">
</a>


## :newspaper: Description

EasyJob - keep and execute your PowerShell, Python, shell and BAT scripts from one interface. Runs on **Windows, macOS and Linux**.


<p align="center">
  <img src="assets/screenshots/main-light.png" alt="EasyJob: script buttons on top, one output card per run below" width="900"/>
</p>

Every run gets its own card (status, duration, PID, its own output, Stop / Run again), so scripts running at the same time never mix. The same app also comes in a dark theme:

<p align="center">
  <img src="assets/screenshots/main-dark.png" alt="EasyJob in the dark theme" width="900"/>
</p>


## :rocket: What's new in 2.0
* Rebuilt on **.NET 10** and [Avalonia UI](https://avaloniaui.net), so the same app runs on Windows, macOS and Linux.
* A new **run panel** replaces the old rich-text console. Every execution gets its own card with status, duration, PID, its own output, and *Stop* / *Run again* / *Copy* / *Collapse* actions. Running several scripts at once no longer mixes their output.
* **Per-tab output**: each tab has its own list of runs. The same script can run several times at once, and *Run in tab* in the button's context menu sends a script's output to another tab.
* Proper **ANSI color** support (PowerShell, git, dotnet, npm, ...).
* **Python** scripts as a script type.
* **Script editor** (Tools → Script editor, or Shift+click a script button): multi-file tabs, syntax coloring for PowerShell, Python, shell, batch, JSON, YAML, XML and Markdown (follows the light/dark theme), find (Ctrl/Cmd+F), save, and **Run (F5)** with live output in a panel under the editor. Run uses what is in the editor right now, so you don't have to save first (unsaved text runs from a temporary copy that is removed when the editor closes).
* **PowerShell command browser** (docked on the right of the script editor; toggle it with the Commands button or Ctrl/Cmd+Shift+P, or open it from Tools → PowerShell commands): search every cmdlet, function and alias of your installed PowerShell, read syntax, description and examples, copy or insert a command into the editor.
* A reorganised menu bar (File, Tabs, Scripts, Tools, View, Help) with keyboard shortcuts and a theme switch (system, light, dark).
* Ready-to-use **test scripts** (`EasyJob/scripts`) and a matching `config.json` for PowerShell, Python, shell and batch, including argument prompts, failures, progress bars and output stress tests.
* **Drag and drop** to reorder script buttons and tabs, right where they are. Tab and script actions (New tab, Add script, Reorder, Paste, rename/remove) are also one click away, no menu digging.
* Refreshed look with light/dark theme following the system, empty states, a collapsible event log, and a unified editor dialog with a file picker.

## :computer: Requirements and running
* [.NET 10 SDK](https://dotnet.microsoft.com/download) to build from source.
* [PowerShell 7 (`pwsh`)](https://learn.microsoft.com/powershell/scripting/install/installing-powershell) for `.ps1` scripts on macOS/Linux (Windows PowerShell also works on Windows).

```
dotnet run --project EasyJob
dotnet publish EasyJob -c Release -r osx-arm64   # or win-x64, linux-x64, ...
```

Script types: `powershell` (`.ps1`), `python` (`.py`, run with `python3 -u`; set `default_python_path` to override), `cmd` (`.bat`/`.cmd` on Windows, run with `/bin/sh` on macOS/Linux) and `shell` (`.sh`).

**Config location.** If a `config.json` sits next to the executable it is used (portable mode); otherwise the app uses `config.json` in your user application-data folder (shown in the status bar). Relative script paths are resolved against the folder that contains `config.json`, and both `\` and `/` separators are accepted, so a config written on Windows works on macOS.

**Ctrl/Cmd + click** a script button to show the script in your file manager, **Shift + click** to open it. Right-click for more actions.

> `EasyJobPSTools` (dialogs for your scripts) is a PowerShell module that works on Windows, macOS and Linux; see below.


## :abacus: Features

### Buttons and tabs
* **Add script** (Ctrl/Cmd+N), **Reorder**, **Paste** and **Filter** (Ctrl/Cmd+F) are at the right end of the tab strip. Right-click a button for _Run in tab_, _Edit in script editor_, _Open with default app_, _Show in folder_, _Edit_, _Copy_ and _Remove_; right-click a tab to rename, remove or reorder it. Everything is saved to your `config.json` as you go.
* **Drag and drop** script buttons and tabs to reorder them.
* **Ctrl/Cmd + click** a button to show its script in your file manager, **Shift + click** to open it in the script editor.

<p align="center">
  <img src="assets/screenshots/add-script.png" alt="Add script dialog with arguments" height="420"/>
  <img src="assets/screenshots/arguments-prompt.png" alt="The prompt a button with arguments shows before it runs" height="200"/>
</p>

* **Arguments**: a button can ask for input every time it runs. Each argument has a prompt; what you type is passed to the script, in order.

### Lots of scripts
Buttons fill the row width. **View → Button size** switches between comfortable cards, a compact layout (about twice as many on screen) and Automatic (compact once a tab has more than 12 buttons; saved as `button_density`). Drag the grip under the script area to make it taller or shorter (double-click resets it), or use the filter.

<p align="center">
  <img src="assets/screenshots/many-scripts.png" alt="30 scripts in compact mode" width="900"/>
</p>

### Script editor
Tools → Script editor (or Shift+click a button): tabs, syntax coloring for PowerShell, Python, shell, batch, JSON, YAML, XML and Markdown (follows the theme), find, and **Run (F5)** with live output under the editor. Run uses exactly what is in the editor, so you do not have to save first. The PowerShell command browser docks on the right (Commands button, Ctrl/Cmd+Shift+P).

<p align="center">
  <img src="assets/screenshots/script-editor.png" alt="Script editor with a finished run" width="900"/>
</p>

### Menus and shortcuts
File, Tabs, Scripts, Tools, View and Help, with the shortcut next to each item. View has the theme (system, light, dark), button size, filter, expand/collapse outputs and the event log.

<p align="center">
  <img src="assets/screenshots/menu-view.png" alt="The View menu" width="700"/>
</p>

### Colors
Output colors are whatever your script or tool prints: ANSI colors (git, dotnet, npm, Python, ...) and, for PowerShell, its own colors (errors, tables, `Write-Host -ForegroundColor`). The option "Use PowerShell's own colors in output" in Tools → Configuration controls the PowerShell part.

### Updates
EasyJob checks the GitHub releases when it starts (switch it off in Configuration, or use Help → Check for updates). When a newer release exists an **Update to x.y.z** button appears in the menu bar; it downloads the release zip, shows the release notes, then restarts into the new version and keeps your `config.json`. Each release must carry a `.zip`; if there are several, the one whose name contains your OS (`win`, `osx`/`macos`, `linux`) and CPU (`x64`, `arm64`) is used. The app folder must be writable, and a `dotnet run` development run cannot update itself.

### Troubleshooting
Help → Troubleshooting lists your system, runtime, config (including script files that are missing), interpreters, environment and recent events, with a copy button for bug reports.


## :hammer: EasyJobPSTools

EasyJobPSTools is a PowerShell module that gives your scripts real windows. EasyJob closes a script's stdin, so `Read-Host` cannot work; use these instead. It works on **Windows, macOS and Linux**: Windows Forms (or the ModernWpf input box when the DLLs are present) on Windows, AppleScript on macOS (nothing to install), and `zenity` on Linux.

```PowerShell
Import-Module (Join-Path $PSScriptRoot '../../EasyJobPSTools/EasyJobPSTools.psd1')

$name  = Show-EJInputBox -Header 'Deploy' -Text 'Your name?' -AllowEmptyResult $false
$env   = Show-EJSelectItem -Header 'Deploy' -Text 'Environment' -Items 'dev','staging','prod'
if (Show-EJConfirm -Header 'Deploy' -Text "Deploy to $env?" -DefaultNo) { ... }
```

| Function | What it does |
| --- | --- |
| `Show-EJInputBox` | Text input. `-DefaultValue`, `-Password`, `-AsSecureString`, `-AllowEmptyResult` |
| `Show-EJMessageBox` | Message with an Information / Warning / Error icon |
| `Show-EJConfirm` | Yes/No question, returns `$true`/`$false` (`-DefaultNo`) |
| `Show-EJSelectItem` | Pick one item, or several with `-MultiSelect` |
| `Show-EJSelectFileWindow` | Open-file window: `-FileType`, `-InitialDirectory`, `-MultiSelect` |
| `Show-EJSaveFileWindow` | Save-file window: `-DefaultName`, `-FileType` |
| `Show-EJSelectFolderWindow` | Folder picker |
| `Show-EJNotification` | Desktop notification |
| `Get-EJDialogBackend` | Which backend is in use (`$null` means no dialogs possible) |

Cancelling gives an empty result (`""`, an empty array, or `$false`); add `-ThrowOnCancel` to get an error instead. `-FileType` takes the Windows syntax (`"Text (*.txt)|*.txt|All files|*.*"`) or a plain list (`"*.txt;*.md"`). The build copies the module next to the app, so scripts in `scripts/powershell` can import `../../EasyJobPSTools/EasyJobPSTools.psd1` (see `dialogs.ps1`).

## :gear: Configuration

Everything is saved in `config.json` (see [Config location](#computer-requirements-and-running)); you can edit it by hand or use Tools → Configuration and the dialogs. A trimmed example:

```json
{
  "default_powershell_path": "",
  "default_python_path": "",
  "default_shell_path": "",
  "powershell_arguments": "",
  "console_background": "#0C0C0C",
  "console_foreground": "#E6E6E6",
  "powershell_native_colors": true,
  "theme": "system",
  "button_density": "auto",
  "check_updates_on_startup": true,
  "clear_events_when_reload": true,
  "restrictions": {
    "block_tabs_remove": false,
    "block_buttons_remove": false,
    "block_buttons_add": false,
    "hide_menu_item_settings_configuration": false
  },
  "tabs": [
    {
      "ID": "10000000-0000-4000-8000-000000000001",
      "Header": "Deploy",
      "Buttons": [
        {
          "Id": "00000000-0000-4000-8000-000000000001",
          "Text": "Deploy",
          "Description": "Deploy a build to an environment",
          "Script": "scripts/deploy.sh",
          "ScriptPathType": "relative",
          "ScriptType": "shell",
          "Arguments": [
            { "ArgumentQuestion": "Which environment?" },
            { "ArgumentQuestion": "Build number" }
          ]
        }
      ]
    }
  ]
}
```

* Interpreter paths can be left empty; EasyJob looks for `pwsh`/`powershell`, `python3`/`python` and `/bin/sh` itself.
* `ScriptType` is `powershell`, `python`, `shell` or `cmd`; `ScriptPathType` is `relative` (to the config folder) or `absolute`.
* `ArgumentQuestion` is the prompt text shown for an argument. Keys written by older versions (`argument_question`, lower-case names) still load.
* `restrictions` lock down or hide parts of the UI (useful when you hand a prepared config to a team). The full list is in `EasyJob/config.json`.
* `ID`/`Id` can be any GUID, or left out; EasyJob generates one.


## :electric_plug: Compilation

1. Install the [.NET 10 SDK](https://dotnet.microsoft.com/download).
2. `dotnet build EasyJob.sln`, or `dotnet run --project EasyJob`.
3. Publish a release for your platform: `dotnet publish EasyJob -c Release -r win-x64` (or `osx-arm64`, `linux-x64`, ...), then zip the output folder as the release asset.

`EasyJobPSTools` is plain PowerShell and is copied next to the app by the build; its optional ModernWpf input box (`EasyJobPSTools/EasyJobPSTools.sln`, Windows, Visual Studio) is not required.

## :dart: Contributing

Contribution is very much appreciated. Hope that this tool might be useful for you!

Thanks to the contributors:

<a href="https://github.com/akshinmustafayev/EasyJob/graphs/contributors">
  <img src="https://contrib.rocks/image?repo=akshinmustafayev/EasyJob" />
</a>

