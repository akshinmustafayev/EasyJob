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
* _Remove_, _Edit_ or _Copy_ button from the GUI by right mouse click on it and then select item in the context menu. Settings are automatically will be saved to your config.json file.

![image](https://raw.githubusercontent.com/akshinmustafayev/EasyJob/main/ej/feature1_1_9_0.png)


* _Remove_, _Rename_ or _Add_ tab from the GUI by right mouse click on it and then select Remove Tab in the context menu. Settings are automatically will be saved to your config.json file.

![image](https://raw.githubusercontent.com/akshinmustafayev/EasyJob/main/ej/feature2_1_1_5.png)


* _Add_ or _Paste_ button from the GUI by right mouse click on button bar.

![image](https://raw.githubusercontent.com/akshinmustafayev/EasyJob/main/ej/feature3_1_1_9_0.PNG)


* Reorder Tabs from the _Settings->Workflow->Reorder_ tabs window
* Add Tabs from the _Settings->Workflow->Add tab_ window
* Rename Tabs from the _Settings->Workflow->Remove current tab_ window
* Remove Tabs from the _Settings->Workflow->Rename current tab_ window
* Remove Add buttons from the _Settings->Workflow->Add button to current_ tab window
* Reorder buttons from the _Settings->Workflow->Reorder buttons in current_ tab window
* Change application settings from the _Settings->Configuration_ window
* **Updates**: EasyJob checks the GitHub releases when it starts (switch it off in Configuration, or use Help → Check for updates). When a newer release exists an **Update to x.y.z** button appears in the menu bar; it downloads the release zip, shows the release notes, then restarts into the new version and keeps your `config.json`. Each release must carry a `.zip`; if there are several, the one whose name contains your OS (`win`, `osx`/`macos`, `linux`) and CPU (`x64`, `arm64`) is used. The app folder must be writable, and a `dotnet run` development run cannot update itself.
* Colored console support
* Lots of scripts? The **Add script**, **Reorder**, **Paste**, **Filter** (Ctrl/Cmd+F) and **compact view** controls live at the right end of the tab strip, so the whole panel is for buttons. Buttons fill the row width; **View → Button size** picks Comfortable cards, Compact (about twice as many on screen) or Automatic (compact once a tab has more than 12 buttons, saved as `button_density`). Drag the grip under the script area to make it taller or shorter (double-click resets it), or use the filter.


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

Configuration could be done from config.json file located with the app executable.

Here is an example:

```
{
  "default_powershell_path": "C:\\Windows\\System32\\WindowsPowerShell\\v1.0\\powershell.exe",
  "default_cmd_path": "C:\\Windows\\System32\\cmd.exe",
  "powershell_arguments": "",
  "console_background": "Black",
  "console_foreground": "White",
  "powershell_native_colors": true,
  "button_density": "auto",
  "check_updates_on_startup": true,
  "clear_events_when_reload": true,
  "restrictions": {
    "block_tabs_remove": false,
    "block_buttons_remove": false,
    "block_tabs_add": false,
    "block_buttons_add": false,
    "block_buttons_reorder": false,
    "block_buttons_edit": false,
    "block_tabs_rename": false,
    "block_buttons_paste": false,
    "block_buttons_copy": false,
    "hide_menu_item_file_reload_config": false,
    "hide_menu_item_file_open_app_folder": false,
    "hide_menu_item_file_clear_events_list": false,
    "hide_menu_item_settings": false,
    "hide_menu_item_settings_workflow": false,
    "hide_menu_item_settings_workflow_reorder_tabs": false,
    "hide_menu_item_settings_workflow_add_tab": false,
    "hide_menu_item_settings_workflow_remove_current_tab": false,
    "hide_menu_item_settings_workflow_rename_current_tab": false,
    "hide_menu_item_settings_workflow_add_button_to_current_tab": false,
    "hide_menu_item_settings_workflow_reorder_buttons_in_current_tab": false,
    "hide_menu_item_settings_configuration": false,
    "hide_menu_item_help": false,
    "hide_menu_item_help_troubleshooting": false,
    "hide_menu_item_help_about": false
  },
  "tabs": [
    {
      "ID": "2e5feab0-527c-451c-b83c-d838d22dacac",
      "header": "Common actions",
      "buttons": [
        {
          "Id": "01bf5871-442e-4f73-91a3-fa13855b609c",
          "text": "test01",
          "description": "Some test script",
          "script": "scripts\\common\\test01.ps1",
          "scriptpathtype": "relative",
          "scripttype": "powershell",
          "arguments": []
        },
        {
          "Id": "9cdc38fa-fc32-4a9d-be78-cd2bfe264422",
          "text": "Bat script",
          "description": "Some BAT script",
          "script": "scripts\\test02.bat",
          "scriptpathtype": "relative",
          "scripttype": "bat",
          "arguments": []
        },
        {
          "Id": "5ec086d9-7987-43ef-84fb-1d8481b05aea",
          "text": "Absolute path script",
          "description": "",
          "script": "C:\\scripts\\absolute_script.ps1",
          "scriptpathtype": "absolute",
          "scripttype": "powershell",
          "arguments": []
        },
        {
          "Id": "c28abef3-494c-48f5-96d8-a5788ced1a23",
          "text": "test04",
          "description": "Some test 04 script with arguments",
          "script": "scripts\\common\\test04.ps1",
          "scriptpathtype": "relative",
          "scripttype": "powershell",
          "arguments": [
            {
              "argument_question": "What is your name?",
              "argument_answer": ""
            },
            {
              "argument_question": "What is your surname",
              "argument_answer": ""
            },
            {
              "argument_question": "No, really what is your name?",
              "argument_answer": ""
            }
          ]
        }
      ]
    },
    {
      "ID": "42f71e1a-32b9-4c16-8c7d-256cd589c52e",
      "header": "Second Tab",
      "buttons": [
        {
          "Id": "3476554c-77b1-4abd-914e-ab1db866fc5f",
          "text": "And this is button too",
          "description": "no description",
          "script": "scripts\\some_button_script.ps1",
          "scriptpathtype": "relative",
          "scripttype": "powershell",
          "arguments": []
        }
      ]
    }
  ]
}
```

_Note 1: Do not specify argument_answer value, since it will be ignored when executing script_

_Note 2: ID my be any other random GUID number. You may not specify it, application will regenerate it after changes if it is absent._


## :triangular_flag_on_post: Easy access
_CTRL+Left Mouse Click_ on the button will open folder where script attached to the button is located

_SHIFT+Left Mouse Click_ on the button will open the script attached to the button with your default ps1 text editor

## :electric_plug: Compilation

1. Download and install Visual Studio 2022
2. Open project in Visual Studio and build it

## :dart: Contributing

Contribution is very much appreciated. Hope that this tool might be useful for you!

Thanks to the contributors:

<a href="https://github.com/akshinmustafayev/EasyJob/graphs/contributors">
  <img src="https://contrib.rocks/image?repo=akshinmustafayev/EasyJob" />
</a>

