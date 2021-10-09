[void][Reflection.Assembly]::LoadWithPartialName('Microsoft.VisualBasic')

function Show-EJInputBox {
    <#
        .SYNOPSIS
        Shows Input-Box for you script.

        .DESCRIPTION
        Shows Input-Box for you script. This might be necessary
        when you want to get som input while executing your script,
        since EasyJob does not support Read from console.

        .PARAMETER Title
        Specifies Title for message box.

        .PARAMETER Message
        Specifies Message for message box.

        .INPUTS
        None.

        .OUTPUTS
        String value of the input from the box.

        .EXAMPLE
        C:\PS> $Result = Show-EJInputBox -Title "Specify your name" -Message "What is your name?"
        C:\PS> Write-Host "Your name is $Result"
        Your name is test

        .LINK
        https://github.com/akshinmustafayev/EasyJob
    #>

    param (
        [Parameter(Mandatory = $true)][ValidateNotNullOrEmpty()] [string]$Title,
        [Parameter(Mandatory = $true)][ValidateNotNullOrEmpty()] [string]$Message
    )
    
    $Result = [Microsoft.VisualBasic.Interaction]::InputBox($Message, $Title)
    return $Result
}