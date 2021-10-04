# EasyJob

<a href="https://img.shields.io/github/license/akshinmustafayev/EasyJob">
  <img src="https://img.shields.io/github/license/akshinmustafayev/EasyJob" alt="License" />
</a>
<a href="https://img.shields.io/tokei/lines/github/akshinmustafayev/EasyJob">
  <img src="https://img.shields.io/tokei/lines/github/akshinmustafayev/EasyJob" alt="Total lines" />
</a>
<a href="https://img.shields.io/github/downloads/akshinmustafayev/EasyJob/total">
  <img src="https://img.shields.io/github/downloads/akshinmustafayev/EasyJob/total" alt="Downloads" />
</a>


## Description

EasyJob keep and execute your PowerShell scripts from one interface


## Overview

<img src="https://github.com/akshinmustafayev/EasyJob/blob/main/ej/1.png?raw=true" alt="EasyJob">
<img src="https://github.com/akshinmustafayev/EasyJob/blob/main/ej/2_arg.png?raw=true" alt="EasyJob">

## Features
* You can remove button from the GUI by right mouse click on it and then select Remove in the context menu. Settings are automatically will be saved to your config.json file.
<img src="https://github.com/akshinmustafayev/EasyJob/blob/main/ej/feature1.PNG?raw=true" alt="EasyJob">

* You can remove tab from the GUI by right mouse click on it and then select Remove Tab in the context menu. Settings are automatically will be saved to your config.json file.
<img src="https://github.com/akshinmustafayev/EasyJob/blob/main/ej/feature2.PNG?raw=true" alt="EasyJob">

## Configuration

Configuration could be done from config.json file located with the app executable.

Here is an example:

```
{
  "default_powershell_path": "C:\\Windows\\System32\\WindowsPowerShell\\v1.0\\powershell.exe",
  "console_background": "Black",
  "console_foreground": "White",
  "clear_events_when_reload": true,
  "tabs": [
    {
      "header": "Common actions",
      "buttons": [
        {
          "text": "test01",
          "description": "Some test script",
          "script": "scripts\\common\\test01.ps1",
          "scriptpathtype": "relative",
          "arguments": []
        },
        {
          "text": "test04",
          "description": "Some test 04 script with arguments",
          "script": "scripts\\common\\test04.ps1",
          "scriptpathtype": "relative",
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
      "header": "Second Tab",
      "buttons": [
        {
          "text": "Some button",
          "description": "no description",
          "script": "scripts\\some_button_script.ps1",
          "scriptpathtype": "relative",
          "arguments": []
        }
      ]
    }
  ]
}
```

_Note: Do not specify argument_answer value, since it will be ignored when executing script_



## Easy access
_CTRL+Left Mouse Click_ on the button will open folder where script attached to the button is located

_SHIFT+Left Mouse Click_ on the button will open the script attached to the button with your default ps1 text editor



## Contributing

Contribution is very much appreciated. Hope that this tool might be useful for you!

Thanks to the contributors:

<a href="https://github.com/akshinmustafayev/EasyJob/graphs/contributors">
  <img src="https://contrib.rocks/image?repo=akshinmustafayev/EasyJob" />
</a>

