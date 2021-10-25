Add-Type -Path "$PSScriptRoot\ModernWpf.dll"
Add-Type -Path "$PSScriptRoot\EasyJobPSTools.dll"

function Show-EJInputBox {
    <#
        .SYNOPSIS
        Shows Input-Box for you script.

        .DESCRIPTION
        Shows Input-Box for you script. This might be necessary
        when you want to get some input while executing your script,
        since EasyJob does not support Read from console.

        .PARAMETER Header
        Specifies Title for input box.

        .PARAMETER Text
        Specifies Text for the input box.

        .PARAMETER AllowEmptyResult
        Specifies if user can not enter any text and press OK.

        .INPUTS
        None.

        .OUTPUTS
        String value of the input from the box.

        .EXAMPLE
        C:\PS> $Result = Show-EJInputBox -Header "Specify your name" -Text "What is your name?" -AllowEmptyResult $false
        C:\PS> Write-Host "Your name is $Result"
        Your name is test

        .LINK
        https://github.com/akshinmustafayev/EasyJobPSTools
        https://github.com/akshinmustafayev/EasyJob
    #>

    param (
        [Parameter(Mandatory = $true)][ValidateNotNullOrEmpty()] [string]$Header,
        [Parameter(Mandatory = $true)][ValidateNotNullOrEmpty()] [string]$Text,
        [Parameter(Mandatory = $true)][ValidateNotNullOrEmpty()] $AllowEmptyResult
    )
    
    $Result = [EasyJobPSTools.Program]::ShowEJInputBoxWindow($Header, $Text, $AllowEmptyResult)
    return $Result
}

function Show-EJSelectFileWindow {
    <#
        .SYNOPSIS
        Shows select file Window and returns selected file path.

        .DESCRIPTION
        Shows select file Window. This might be necessary
        when you want to get selected file path and use it
        in your script.

        .PARAMETER FileType
        Specifies File type which you would like for user to select.
        This parameter may be empty. If it is empty then any file type is espected

        .INPUTS
        None.

        .OUTPUTS
        String path value of the selected file.

        .EXAMPLE
        C:\PS> $Result = Show-EJSelectFileWindow
        D:\temp\excel_list.xlsx

        .EXAMPLE
        C:\PS> $Result = Show-EJSelectFileWindow -FileType "txt files (*.txt)|*.txt|All files (*.*)|*.*"
        D:\temp\testfile.txt

        .LINK
        https://docs.microsoft.com/en-us/dotnet/api/system.windows.forms.filedialog.filter?view=windowsdesktop-5.0#System_Windows_Forms_FileDialog_Filter
        https://github.com/akshinmustafayev/EasyJobPSTools
        https://github.com/akshinmustafayev/EasyJob
    #>

    param (
        [Parameter(Mandatory = $false)] [string]$FileType
    )

    if($null -eq $FileType){
        $Result = [EasyJobPSTools.Program]::ShowEJSelectFileWindow()
        return $Result
    }
    else{
        $Result = [EasyJobPSTools.Program]::ShowEJSelectFileWindow($FileType)
        return $Result
    }
}

function Show-EJSelectFolderWindow {
    <#
        .SYNOPSIS
        Shows select folder Window and returns selected folder path.

        .DESCRIPTION
        Shows select folder Window. This might be necessary
        when you want to get selected folder path and use it
        in your script.

        .INPUTS
        None.

        .OUTPUTS
        String path value of the selected folder.

        .EXAMPLE
        C:\PS> $Result = Show-EJSelectFolderWindow
        D:\temp\

        .LINK
        https://github.com/akshinmustafayev/EasyJobPSTools
        https://github.com/akshinmustafayev/EasyJob
    #>

    $Result = [EasyJobPSTools.Program]::ShowEJSelectFolderWindow()
    return $Result
}
