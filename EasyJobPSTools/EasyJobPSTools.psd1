@{
    RootModule            = 'EasyJobPSTools.psm1'
    ModuleVersion         = '2.0.0'
    GUID                  = 'd6c56376-ae08-4c23-b901-9bc75144e0c6'
    Author                = 'Akshin Mustafayev'
    CompanyName           = 'Akshin Mustafayev'
    Copyright             = '(c) 2021 Akshin Mustafayev. GNU Affero General Public License v3.0.'
    Description           = 'Native dialogs (input, message, confirm, list, file, folder, notification) for scripts run from EasyJob on Windows, macOS and Linux.'
    PowerShellVersion     = '5.1'
    CompatiblePSEditions  = @('Desktop', 'Core')

    FunctionsToExport     = @(
        'Show-EJInputBox', 'Show-EJMessageBox', 'Show-EJConfirm', 'Show-EJSelectItem',
        'Show-EJSelectFileWindow', 'Show-EJSaveFileWindow', 'Show-EJSelectFolderWindow',
        'Show-EJNotification', 'Get-EJDialogBackend'
    )
    CmdletsToExport       = @()
    VariablesToExport     = @()
    AliasesToExport       = @()

    PrivateData           = @{
        PSData = @{
            Tags         = @('EasyJob', 'dialogs', 'gui', 'InputBox', 'Windows', 'macOS', 'Linux')
            ProjectUri   = 'https://github.com/akshinmustafayev/EasyJob'
            ReleaseNotes = '2.0: works on macOS (AppleScript) and Linux (zenity) as well as Windows. New: Show-EJMessageBox, Show-EJConfirm, Show-EJSelectItem, Show-EJSaveFileWindow, Show-EJNotification, Get-EJDialogBackend; -DefaultValue/-Password/-AsSecureString/-ThrowOnCancel on dialogs; multi-select, initial folder and filters for file dialogs. Fixed: Show-EJSelectFileWindow ignored the "no filter" case.'
        }
    }
}
