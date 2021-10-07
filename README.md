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
<a href="https://img.shields.io/github/stars/akshinmustafayev/EasyJob?style=social">
  <img alt="GitHub repo file count" src="https://img.shields.io/github/stars/akshinmustafayev/EasyJob?style=social">
</a>
<a href="https://img.shields.io/github/contributors/akshinmustafayev/EasyJob">
  <img alt="GitHub repo file count" src="https://img.shields.io/github/contributors/akshinmustafayev/EasyJob">
</a>


## Description

EasyJob keep and execute your PowerShell and BAT scripts from one interface


## Overview
![image](https://user-images.githubusercontent.com/29357955/136437666-040d0491-52e8-4c48-a7e9-350e9beb22cb.png)

![image](https://user-images.githubusercontent.com/29357955/136437696-793ccaa8-092b-46b8-bf19-0b054ad784dc.png)

## Features
* You can remove button from the GUI by right mouse click on it and then select Remove in the context menu. Settings are automatically will be saved to your config.json file.

![image](https://user-images.githubusercontent.com/29357955/136437593-22c3c27c-f369-401b-8a90-735daa1a609f.png)

* You can remove tab from the GUI by right mouse click on it and then select Remove Tab in the context menu. Settings are automatically will be saved to your config.json file.

![image](https://user-images.githubusercontent.com/29357955/136437621-9029cc73-0ba8-436b-af73-14656c0f41fc.png)

## Configuration

Configuration could be done from config.json file located with the app executable.

Here is an example:

```
{
  "default_powershell_path": "C:\\Windows\\System32\\WindowsPowerShell\\v1.0\\powershell.exe",
  "default_cmd_path": "C:\\Windows\\System32\\cmd.exe",
  "powershell_arguments": "",
  "console_background": "Black",
  "console_foreground": "White",
  "clear_events_when_reload": true,
  "restrictions": {
    "block_tabs_remove": false,
    "block_buttons_remove": true
  },
  "tabs": [
    {
      "header": "Common actions",
      "buttons": [
        {
          "text": "test01",
          "description": "Some test script",
          "script": "scripts\\common\\test01.ps1",
          "scriptpathtype": "relative",
          "scripttype": "powershell",
          "arguments": []
        },
        {
          "text": "Bat script",
          "description": "Some BAT script",
          "script": "scripts\\test02.bat",
          "scriptpathtype": "relative",
          "scripttype": "bat",
          "arguments": []
        },
        {
          "text": "Absolute path script",
          "description": "",
          "script": "C:\\scripts\\absolute_script.ps1",
          "scriptpathtype": "absolute",
          "scripttype": "powershell",
          "arguments": []
        },
        {
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
      "header": "Second Tab",
      "buttons": [
        {
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

