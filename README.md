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
![image](https://user-images.githubusercontent.com/29357955/136666562-90ae503d-f60c-4528-a2ae-db3b666d14ae.png)

![image](https://user-images.githubusercontent.com/29357955/136666569-27cd5987-8a63-4ded-973a-942f655ce5a0.png)

![image](https://user-images.githubusercontent.com/29357955/136707109-41022bd8-7966-4a8d-ae30-4ce3acb230c9.png)

![image](https://user-images.githubusercontent.com/29357955/136707408-518324f0-1de3-4b66-9d77-ed186d25c1fe.png)


## Features
* You can _Remove_ or _Edit_ button from the GUI by right mouse click on it and then select item in the context menu. Settings are automatically will be saved to your config.json file.

![image](https://user-images.githubusercontent.com/29357955/136830752-7c1ab401-3f90-4421-b0d5-21468ac80d08.png)


* You can _Remove_ or _Add_ tab from the GUI by right mouse click on it and then select Remove Tab in the context menu. Settings are automatically will be saved to your config.json file.

![image](https://user-images.githubusercontent.com/29357955/136830773-9adaf839-fb6f-4019-b537-3d332df94481.png)


* You can _Add_ button from the GUI by right mouse click on button bar and then select Add button.

![image](https://user-images.githubusercontent.com/29357955/136830784-270440e0-7cd5-4529-b71a-fda6b874ec45.png)


* You can reorder Tabs from the _Settings->Workflow->Reorder_ tabs context menu
* You can add Tabs from the _Settings->Workflow->Add tab_ context menu
* You can rename Tabs from the _Settings->Workflow->Remove current tab_ context menu
* You can remove Tabs from the _Settings->Workflow->Rename current tab_ context menu
* You can remove Add buttons from the _Settings->Workflow->Add button to current_ tab context menu


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
    "block_buttons_remove": false,
    "block_tabs_add": false,
    "block_buttons_add": false,
    "block_buttons_edit": false
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

_Note 2: ID my be any other random GUID number. You may not specify it, application will regenerate it after changes if it is absent or not present._


## Easy access
_CTRL+Left Mouse Click_ on the button will open folder where script attached to the button is located

_SHIFT+Left Mouse Click_ on the button will open the script attached to the button with your default ps1 text editor



## Contributing

Contribution is very much appreciated. Hope that this tool might be useful for you!

Thanks to the contributors:

<a href="https://github.com/akshinmustafayev/EasyJob/graphs/contributors">
  <img src="https://contrib.rocks/image?repo=akshinmustafayev/EasyJob" />
</a>

