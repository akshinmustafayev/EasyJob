# EasyJobPSTools - native dialogs for scripts started from EasyJob.
#
# EasyJob runs scripts without a console and closes their stdin, so Read-Host cannot work. These functions show a
# real window instead:
#   Windows : the ModernWpf input box when EasyJobPSTools.dll is present, otherwise Windows Forms
#   macOS   : AppleScript dialogs through osascript (nothing to install)
#   Linux   : zenity (install it with your package manager)
#
# Cancelling a dialog gives an empty result ("" / empty array / $false). Pass -ThrowOnCancel to get an error instead.

$script:EJIsWindows = ($PSVersionTable.PSEdition -eq 'Desktop') -or (($null -ne $IsWindows) -and $IsWindows)
$script:EJIsMac = ($null -ne $IsMacOS) -and $IsMacOS
$script:EJIsLinux = ($null -ne $IsLinux) -and $IsLinux

# The WPF input box is optional: it only loads on Windows when its assemblies sit next to this file.
$script:EJWpf = $false
if ($script:EJIsWindows -and (Test-Path "$PSScriptRoot\ModernWpf.dll") -and (Test-Path "$PSScriptRoot\EasyJobPSTools.dll")) {
    try {
        Add-Type -Path "$PSScriptRoot\ModernWpf.dll"
        Add-Type -Path "$PSScriptRoot\EasyJobPSTools.dll"
        $script:EJWpf = $true
    }
    catch {
        $script:EJWpf = $false
    }
}

# ------------------------------------------------------------------ helpers

function Get-EJDialogBackend {
    <#
        .SYNOPSIS
        Tells which dialog backend is available on this machine.

        .DESCRIPTION
        Returns 'WPF', 'WinForms', 'AppleScript' or 'Zenity', or $null when no dialog can be shown
        (for example Linux without zenity).

        .EXAMPLE
        PS> Get-EJDialogBackend
        AppleScript
    #>
    [CmdletBinding()]
    param()

    if ($script:EJIsWindows) {
        if ($script:EJWpf) { return 'WPF' }
        return 'WinForms'
    }
    if ($script:EJIsMac) {
        if (Get-Command osascript -ErrorAction SilentlyContinue) { return 'AppleScript' }
        return $null
    }
    if (Get-Command zenity -ErrorAction SilentlyContinue) { return 'Zenity' }
    return $null
}

function Assert-EJBackend {
    $backend = Get-EJDialogBackend
    if ($null -eq $backend) {
        if ($script:EJIsLinux) { throw 'EasyJobPSTools needs zenity to show dialogs on Linux. Install it (for example: sudo apt install zenity).' }
        throw 'EasyJobPSTools could not find a dialog backend on this machine.'
    }
    return $backend
}

function Complete-EJCancel {
    param([bool]$ThrowOnCancel)
    if ($ThrowOnCancel) {
        throw (New-Object System.OperationCanceledException 'The dialog was cancelled.')
    }
}

function ConvertTo-EJAppleScript {
    param([string]$Value)
    if ($null -eq $Value) { $Value = '' }
    $escaped = $Value.Replace('\', '\\').Replace('"', '\"').Replace("`r`n", "`n").Replace("`r", "`n")
    if ($escaped.Contains("`n")) {
        return '("' + $escaped.Replace("`n", '" & linefeed & "') + '")'
    }
    return '"' + $escaped + '"'
}

function Invoke-EJNative {
    param([string]$File, [string[]]$Arguments)
    $output = & $File @Arguments 2>$null
    $code = $LASTEXITCODE
    [pscustomobject]@{ ExitCode = $code; Output = (@($output) -join "`n") }
}

function Invoke-EJOsascript {
    param([string]$Script)
    Invoke-EJNative -File 'osascript' -Arguments @('-e', $Script)
}

function Invoke-EJWinDialog {
    # Windows dialogs need an STA thread. pwsh and powershell.exe are STA by default; when they are not,
    # the dialog runs in its own STA runspace.
    param([scriptblock]$Dialog, [object[]]$ArgumentList = @())

    if ([System.Threading.Thread]::CurrentThread.GetApartmentState() -eq 'STA') {
        return @(& $Dialog @ArgumentList)
    }

    $runspace = [runspacefactory]::CreateRunspace()
    $runspace.ApartmentState = 'STA'
    $runspace.ThreadOptions = 'ReuseThread'
    $runspace.Open()
    try {
        $ps = [powershell]::Create()
        $ps.Runspace = $runspace
        [void]$ps.AddScript($Dialog.ToString())
        foreach ($a in $ArgumentList) { [void]$ps.AddArgument($a) }
        $result = @($ps.Invoke())
        if ($ps.Streams.Error.Count -gt 0) { throw $ps.Streams.Error[0] }
        return $result
    }
    finally {
        $runspace.Dispose()
    }
}

function ConvertFrom-EJFileFilter {
    # Accepts the Windows filter syntax ("Text (*.txt)|*.txt|All files|*.*") or a bare pattern list ("*.txt;*.md").
    param([string]$Filter)

    if ([string]::IsNullOrWhiteSpace($Filter)) { return @() }
    if ($Filter -notmatch '\|') {
        $patterns = @($Filter.Split(';') | ForEach-Object { $_.Trim() } | Where-Object { $_ })
        return @([pscustomobject]@{ Name = 'Files'; Patterns = $patterns })
    }

    $parts = $Filter.Split('|')
    $result = @()
    for ($i = 0; ($i + 1) -lt $parts.Count; $i += 2) {
        $patterns = @($parts[$i + 1].Split(';') | ForEach-Object { $_.Trim() } | Where-Object { $_ })
        $result += [pscustomobject]@{ Name = $parts[$i].Trim(); Patterns = $patterns }
    }
    return $result
}

function Get-EJFilterExtensions {
    # File extensions for macOS "of type {...}". Returns nothing when any pattern is not a plain "*.ext" (no restriction).
    param($Filters)

    $extensions = @()
    foreach ($filter in $Filters) {
        foreach ($pattern in $filter.Patterns) {
            if ($pattern -match '^\*\.([A-Za-z0-9_\-]+)$') { $extensions += $Matches[1] }
            else { return @() }
        }
    }
    return @($extensions | Select-Object -Unique)
}

function Read-EJInputNative {
    param([string]$Header, [string]$Text, [string]$Default, [bool]$Password)

    $backend = Assert-EJBackend
    switch ($backend) {
        'AppleScript' {
            $as = 'text returned of (display dialog ' + (ConvertTo-EJAppleScript $Text) + ' default answer ' + (ConvertTo-EJAppleScript $Default) +
                  ' with title ' + (ConvertTo-EJAppleScript $Header)
            if ($Password) { $as += ' with hidden answer' }
            $as += ' buttons {"Cancel", "OK"} default button "OK" cancel button "Cancel")'
            $r = Invoke-EJOsascript $as
            if ($r.ExitCode -ne 0) { return $null }
            return $r.Output
        }
        'Zenity' {
            $zargs = @('--entry', "--title=$Header", "--text=$Text", '--no-markup')
            if ($Default) { $zargs += "--entry-text=$Default" }
            if ($Password) { $zargs += '--hide-text' }
            $r = Invoke-EJNative -File 'zenity' -Arguments $zargs
            if ($r.ExitCode -ne 0) { return $null }
            return $r.Output
        }
        default {
            $r = @(Invoke-EJWinDialog -Dialog $script:EJWinInput -ArgumentList @($Header, $Text, $Default, $Password))
            if ($r.Count -eq 0) { return $null }
            return [string]$r[0]
        }
    }
}

$script:EJWinInput = {
    param($Header, $Text, $Default, $Password)
    Add-Type -AssemblyName System.Windows.Forms
    Add-Type -AssemblyName System.Drawing
    [System.Windows.Forms.Application]::EnableVisualStyles()

    $form = New-Object System.Windows.Forms.Form
    $form.Text = $Header
    $form.StartPosition = 'CenterScreen'
    $form.TopMost = $true
    $form.FormBorderStyle = 'FixedDialog'
    $form.MaximizeBox = $false
    $form.MinimizeBox = $false
    $form.ClientSize = New-Object System.Drawing.Size(420, 172)

    $label = New-Object System.Windows.Forms.Label
    $label.Text = $Text
    $label.SetBounds(16, 14, 388, 60)

    $box = New-Object System.Windows.Forms.TextBox
    $box.SetBounds(16, 84, 388, 24)
    $box.Text = $Default
    $box.UseSystemPasswordChar = [bool]$Password

    $ok = New-Object System.Windows.Forms.Button
    $ok.Text = 'OK'
    $ok.DialogResult = 'OK'
    $ok.SetBounds(238, 130, 80, 28)

    $cancel = New-Object System.Windows.Forms.Button
    $cancel.Text = 'Cancel'
    $cancel.DialogResult = 'Cancel'
    $cancel.SetBounds(324, 130, 80, 28)

    $form.AcceptButton = $ok
    $form.CancelButton = $cancel
    $form.Controls.AddRange(@($label, $box, $ok, $cancel))
    $form.Add_Shown({ $box.Focus(); $box.SelectAll() })

    $result = $form.ShowDialog()
    $value = $box.Text
    $form.Dispose()
    if ($result -eq 'OK') { return $value }
}

# ------------------------------------------------------------------ dialogs

function Show-EJInputBox {
    <#
        .SYNOPSIS
        Shows an input box for your script.

        .DESCRIPTION
        Shows an input box. Use it when you need some input while the script runs, since EasyJob does not
        support Read-Host.

        .PARAMETER Header
        Title of the window.

        .PARAMETER Text
        Text shown above the input field.

        .PARAMETER AllowEmptyResult
        Whether the user may press OK without typing anything. Default: $true.

        .PARAMETER DefaultValue
        Text the field starts with.

        .PARAMETER Password
        Hides what is typed.

        .PARAMETER AsSecureString
        Returns a SecureString (implies -Password).

        .PARAMETER ThrowOnCancel
        Throw instead of returning an empty string when the user cancels.

        .OUTPUTS
        The text, or "" when cancelled.

        .EXAMPLE
        PS> $name = Show-EJInputBox -Header "Specify your name" -Text "What is your name?" -AllowEmptyResult $false
        PS> Write-Host "Your name is $name"

        .EXAMPLE
        PS> $token = Show-EJInputBox -Header "Login" -Text "API token" -Password -AsSecureString

        .LINK
        https://github.com/akshinmustafayev/EasyJob
    #>
    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $true, Position = 0)][ValidateNotNullOrEmpty()][string]$Header,
        [Parameter(Mandatory = $true, Position = 1)][ValidateNotNullOrEmpty()][string]$Text,
        [Parameter(Position = 2)][bool]$AllowEmptyResult = $true,
        [string]$DefaultValue = '',
        [switch]$Password,
        [switch]$AsSecureString,
        [switch]$ThrowOnCancel
    )

    $hidden = $Password.IsPresent -or $AsSecureString.IsPresent
    $answer = $null

    if ($script:EJWpf -and -not $hidden -and -not $DefaultValue -and -not $ThrowOnCancel) {
        # The compiled ModernWpf window validates and cancels on its own ("" for both an empty value and cancel).
        return [EasyJobPSTools.Program]::ShowEJInputBoxWindow($Header, $Text, $AllowEmptyResult)
    }

    Assert-EJBackend | Out-Null
    $prompt = $Text
    while ($true) {
        $answer = Read-EJInputNative -Header $Header -Text $prompt -Default $DefaultValue -Password $hidden
        if ($null -eq $answer) {
            Complete-EJCancel $ThrowOnCancel.IsPresent
            return ''
        }
        if ($answer.Length -gt 0 -or $AllowEmptyResult) { break }
        $prompt = $Text + "`n`nPlease enter a value."
    }

    if ($AsSecureString) { return (ConvertTo-SecureString -String $answer -AsPlainText -Force) }
    return $answer
}

function Show-EJMessageBox {
    <#
        .SYNOPSIS
        Shows a message and waits for OK.

        .PARAMETER Header
        Title of the window.

        .PARAMETER Text
        The message.

        .PARAMETER Icon
        Information (default), Warning, Error or None.

        .EXAMPLE
        PS> Show-EJMessageBox -Header "Deploy" -Text "Finished successfully."
    #>
    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $true, Position = 0)][string]$Header,
        [Parameter(Mandatory = $true, Position = 1)][string]$Text,
        [ValidateSet('Information', 'Warning', 'Error', 'None')][string]$Icon = 'Information'
    )

    switch (Assert-EJBackend) {
        'AppleScript' {
            $symbol = @{ Information = ' with icon note'; Warning = ' with icon caution'; Error = ' with icon stop'; None = '' }[$Icon]
            [void](Invoke-EJOsascript ('display dialog ' + (ConvertTo-EJAppleScript $Text) + ' with title ' + (ConvertTo-EJAppleScript $Header) +
                ' buttons {"OK"} default button "OK"' + $symbol))
        }
        'Zenity' {
            $kind = @{ Information = '--info'; Warning = '--warning'; Error = '--error'; None = '--info' }[$Icon]
            [void](Invoke-EJNative -File 'zenity' -Arguments @($kind, "--title=$Header", "--text=$Text", '--no-markup'))
        }
        default {
            [void](Invoke-EJWinDialog -Dialog {
                param($Header, $Text, $Icon)
                Add-Type -AssemblyName System.Windows.Forms
                [void][System.Windows.Forms.MessageBox]::Show($Text, $Header, 'OK', $Icon)
            } -ArgumentList @($Header, $Text, $Icon))
        }
    }
}

function Show-EJConfirm {
    <#
        .SYNOPSIS
        Asks a yes/no question and returns $true or $false.

        .PARAMETER Header
        Title of the window.

        .PARAMETER Text
        The question.

        .PARAMETER DefaultNo
        Make "No" the default answer (useful before destructive actions).

        .EXAMPLE
        PS> if (Show-EJConfirm -Header "Delete" -Text "Delete all logs?" -DefaultNo) { Remove-Item .\logs -Recurse }
    #>
    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $true, Position = 0)][string]$Header,
        [Parameter(Mandatory = $true, Position = 1)][string]$Text,
        [switch]$DefaultNo
    )

    switch (Assert-EJBackend) {
        'AppleScript' {
            $default = if ($DefaultNo) { 'No' } else { 'Yes' }
            $r = Invoke-EJOsascript ('button returned of (display dialog ' + (ConvertTo-EJAppleScript $Text) + ' with title ' + (ConvertTo-EJAppleScript $Header) +
                ' buttons {"No", "Yes"} default button "' + $default + '" cancel button "No" with icon caution)')
            return ($r.ExitCode -eq 0 -and $r.Output -eq 'Yes')
        }
        'Zenity' {
            $zargs = @('--question', "--title=$Header", "--text=$Text", '--no-markup')
            if ($DefaultNo) { $zargs += '--default-cancel' }
            $r = Invoke-EJNative -File 'zenity' -Arguments $zargs
            return ($r.ExitCode -eq 0)
        }
        default {
            $r = @(Invoke-EJWinDialog -Dialog {
                param($Header, $Text, $DefaultNo)
                Add-Type -AssemblyName System.Windows.Forms
                $button = if ($DefaultNo) { 'Button2' } else { 'Button1' }
                [System.Windows.Forms.MessageBox]::Show($Text, $Header, 'YesNo', 'Question', $button) -eq 'Yes'
            } -ArgumentList @($Header, $Text, $DefaultNo.IsPresent))
            return ($r.Count -gt 0 -and [bool]$r[0])
        }
    }
}

$script:EJWinSelect = {
    param($Header, $Text, $Items, $Multi)
    Add-Type -AssemblyName System.Windows.Forms
    Add-Type -AssemblyName System.Drawing
    [System.Windows.Forms.Application]::EnableVisualStyles()

    $form = New-Object System.Windows.Forms.Form
    $form.Text = $Header
    $form.StartPosition = 'CenterScreen'
    $form.TopMost = $true
    $form.FormBorderStyle = 'FixedDialog'
    $form.MaximizeBox = $false
    $form.MinimizeBox = $false
    $form.ClientSize = New-Object System.Drawing.Size(420, 330)

    $label = New-Object System.Windows.Forms.Label
    $label.Text = $Text
    $label.SetBounds(16, 14, 388, 36)

    $list = New-Object System.Windows.Forms.ListBox
    $list.SetBounds(16, 54, 388, 218)
    $list.SelectionMode = if ($Multi) { 'MultiExtended' } else { 'One' }
    foreach ($item in $Items) { [void]$list.Items.Add([string]$item) }

    $ok = New-Object System.Windows.Forms.Button
    $ok.Text = 'OK'
    $ok.DialogResult = 'OK'
    $ok.Enabled = $false
    $ok.SetBounds(238, 288, 80, 28)

    $cancel = New-Object System.Windows.Forms.Button
    $cancel.Text = 'Cancel'
    $cancel.DialogResult = 'Cancel'
    $cancel.SetBounds(324, 288, 80, 28)

    $list.Add_SelectedIndexChanged({ $ok.Enabled = ($list.SelectedItems.Count -gt 0) })
    $list.Add_DoubleClick({ if ($list.SelectedItems.Count -gt 0) { $ok.PerformClick() } })

    $form.AcceptButton = $ok
    $form.CancelButton = $cancel
    $form.Controls.AddRange(@($label, $list, $ok, $cancel))

    $result = $form.ShowDialog()
    $selected = @($list.SelectedItems | ForEach-Object { [string]$_ })
    $form.Dispose()
    if ($result -eq 'OK') { return $selected }
}

function Show-EJSelectItem {
    <#
        .SYNOPSIS
        Lets the user pick one (or several) items from a list.

        .PARAMETER Header
        Title of the window.

        .PARAMETER Text
        Text shown above the list.

        .PARAMETER Items
        The items to choose from. They are shown with their string value.

        .PARAMETER MultiSelect
        Allow selecting more than one item.

        .PARAMETER ThrowOnCancel
        Throw instead of returning an empty result when the user cancels.

        .OUTPUTS
        The chosen item (an array with -MultiSelect). "" / an empty array when cancelled.

        .EXAMPLE
        PS> $env = Show-EJSelectItem -Header "Deploy" -Text "Target environment" -Items 'dev','staging','prod'

        .EXAMPLE
        PS> $services = Show-EJSelectItem -Header "Restart" -Text "Services" -Items (Get-Service).Name -MultiSelect
    #>
    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $true, Position = 0)][string]$Header,
        [Parameter(Mandatory = $true, Position = 1)][string]$Text,
        [Parameter(Mandatory = $true, Position = 2)][object[]]$Items,
        [switch]$MultiSelect,
        [switch]$ThrowOnCancel
    )

    $labels = @($Items | ForEach-Object { [string]$_ })
    if ($labels.Count -eq 0) { throw 'Show-EJSelectItem needs at least one item.' }

    $chosen = @()
    switch (Assert-EJBackend) {
        'AppleScript' {
            $list = ($labels | ForEach-Object { ConvertTo-EJAppleScript $_ }) -join ', '
            $as = 'set r to choose from list {' + $list + '} with title ' + (ConvertTo-EJAppleScript $Header) + ' with prompt ' + (ConvertTo-EJAppleScript $Text)
            if ($MultiSelect) { $as += ' with multiple selections allowed' }
            $as += "`nif r is false then return `"`"`nset AppleScript's text item delimiters to linefeed`nreturn r as text"
            $r = Invoke-EJOsascript $as
            if ($r.ExitCode -eq 0 -and $r.Output.Length -gt 0) { $chosen = @($r.Output.Split("`n")) }
        }
        'Zenity' {
            $sep = [string][char]31
            $zargs = @('--list', "--title=$Header", "--text=$Text", '--no-markup', '--column=Item', '--hide-header', "--separator=$sep")
            if ($MultiSelect) { $zargs += '--multiple' }
            $zargs += $labels
            $r = Invoke-EJNative -File 'zenity' -Arguments $zargs
            if ($r.ExitCode -eq 0 -and $r.Output.Length -gt 0) { $chosen = @($r.Output.Split([char]31)) }
        }
        default {
            $chosen = @(Invoke-EJWinDialog -Dialog $script:EJWinSelect -ArgumentList @($Header, $Text, [string[]]$labels, $MultiSelect.IsPresent))
        }
    }

    if ($chosen.Count -eq 0) {
        Complete-EJCancel $ThrowOnCancel.IsPresent
        if ($MultiSelect) { return , @() }
        return ''
    }

    # Hand back the original objects, not their text.
    $result = @()
    foreach ($label in $chosen) {
        $match = @($Items | Where-Object { [string]$_ -eq $label })
        if ($match.Count -gt 0) { $result += $match[0] } else { $result += $label }
    }
    if ($MultiSelect) { return , $result }
    return $result[0]
}

function Show-EJSelectFileWindow {
    <#
        .SYNOPSIS
        Shows a select-file window and returns the selected file path.

        .PARAMETER FileType
        Optional filter. Either the Windows syntax "Text files (*.txt)|*.txt|All files (*.*)|*.*"
        or a plain pattern list such as "*.txt;*.md". Empty means any file.

        .PARAMETER InitialDirectory
        Folder the window opens in.

        .PARAMETER Title
        Window title.

        .PARAMETER MultiSelect
        Allow selecting several files; the result is then an array of paths.

        .PARAMETER ThrowOnCancel
        Throw instead of returning an empty result when the user cancels.

        .OUTPUTS
        The path (an array with -MultiSelect), or "" / an empty array when cancelled.

        .EXAMPLE
        PS> $file = Show-EJSelectFileWindow -FileType "Text files (*.txt)|*.txt|All files (*.*)|*.*"

        .EXAMPLE
        PS> $files = Show-EJSelectFileWindow -FileType "*.csv" -MultiSelect -InitialDirectory ~/Downloads
    #>
    [CmdletBinding()]
    param (
        [Parameter(Position = 0)][string]$FileType = '',
        [string]$InitialDirectory = '',
        [string]$Title = 'Select a file',
        [switch]$MultiSelect,
        [switch]$ThrowOnCancel
    )

    $filters = @(ConvertFrom-EJFileFilter $FileType)
    $dir = $null
    if ($InitialDirectory -and (Test-Path -LiteralPath $InitialDirectory -PathType Container)) {
        $dir = (Resolve-Path -LiteralPath $InitialDirectory).ProviderPath
    }

    $paths = @()
    switch (Assert-EJBackend) {
        'AppleScript' {
            $as = 'set r to choose file with prompt ' + (ConvertTo-EJAppleScript $Title)
            $extensions = @(Get-EJFilterExtensions $filters)
            if ($extensions.Count -gt 0) { $as += ' of type {' + (($extensions | ForEach-Object { ConvertTo-EJAppleScript $_ }) -join ', ') + '}' }
            if ($dir) { $as += ' default location (POSIX file ' + (ConvertTo-EJAppleScript $dir) + ' as alias)' }
            if ($MultiSelect) { $as += ' with multiple selections allowed' }
            $as += "`nif class of r is not list then set r to {r}`nset out to {}`nrepeat with f in r`nset end of out to POSIX path of (contents of f)`nend repeat`nset AppleScript's text item delimiters to linefeed`nreturn out as text"
            $r = Invoke-EJOsascript $as
            if ($r.ExitCode -eq 0 -and $r.Output.Length -gt 0) { $paths = @($r.Output.Split("`n")) }
        }
        'Zenity' {
            $sep = [string][char]31
            $zargs = @('--file-selection', "--title=$Title", "--separator=$sep")
            foreach ($f in $filters) { $zargs += ('--file-filter=' + $f.Name + ' | ' + ($f.Patterns -join ' ')) }
            if ($dir) { $zargs += ('--filename=' + $dir.TrimEnd('/') + '/') }
            if ($MultiSelect) { $zargs += '--multiple' }
            $r = Invoke-EJNative -File 'zenity' -Arguments $zargs
            if ($r.ExitCode -eq 0 -and $r.Output.Length -gt 0) { $paths = @($r.Output.Split([char]31)) }
        }
        default {
            $paths = @(Invoke-EJWinDialog -Dialog {
                param($Title, $Filter, $Dir, $Multi)
                Add-Type -AssemblyName System.Windows.Forms
                $dialog = New-Object System.Windows.Forms.OpenFileDialog
                $dialog.Title = $Title
                if ($Filter) { $dialog.Filter = $Filter }
                if ($Dir) { $dialog.InitialDirectory = $Dir }
                $dialog.Multiselect = [bool]$Multi
                if ($dialog.ShowDialog() -eq 'OK') { return $dialog.FileNames }
            } -ArgumentList @($Title, (ConvertTo-EJWinFilter $filters), $dir, $MultiSelect.IsPresent))
        }
    }

    if ($paths.Count -eq 0) {
        Complete-EJCancel $ThrowOnCancel.IsPresent
        if ($MultiSelect) { return , @() }
        return ''
    }
    if ($MultiSelect) { return , ([string[]]$paths) }
    return [string]$paths[0]
}

function ConvertTo-EJWinFilter {
    param($Filters)
    if (@($Filters).Count -eq 0) { return '' }
    return ((@($Filters) | ForEach-Object { $_.Name + '|' + ($_.Patterns -join ';') }) -join '|')
}

function Show-EJSaveFileWindow {
    <#
        .SYNOPSIS
        Shows a save-file window and returns the chosen path (the file is not created).

        .PARAMETER FileType
        Optional filter, same syntax as Show-EJSelectFileWindow.

        .PARAMETER DefaultName
        File name suggested to the user.

        .PARAMETER InitialDirectory
        Folder the window opens in.

        .PARAMETER Title
        Window title.

        .PARAMETER ThrowOnCancel
        Throw instead of returning "" when the user cancels.

        .EXAMPLE
        PS> $target = Show-EJSaveFileWindow -DefaultName "report.csv" -FileType "CSV (*.csv)|*.csv"
        PS> if ($target) { $rows | Export-Csv $target }
    #>
    [CmdletBinding()]
    param (
        [Parameter(Position = 0)][string]$FileType = '',
        [string]$DefaultName = '',
        [string]$InitialDirectory = '',
        [string]$Title = 'Save file',
        [switch]$ThrowOnCancel
    )

    $filters = @(ConvertFrom-EJFileFilter $FileType)
    $dir = $null
    if ($InitialDirectory -and (Test-Path -LiteralPath $InitialDirectory -PathType Container)) {
        $dir = (Resolve-Path -LiteralPath $InitialDirectory).ProviderPath
    }

    $path = $null
    switch (Assert-EJBackend) {
        'AppleScript' {
            $as = 'POSIX path of (choose file name with prompt ' + (ConvertTo-EJAppleScript $Title)
            if ($DefaultName) { $as += ' default name ' + (ConvertTo-EJAppleScript $DefaultName) }
            if ($dir) { $as += ' default location (POSIX file ' + (ConvertTo-EJAppleScript $dir) + ' as alias)' }
            $as += ')'
            $r = Invoke-EJOsascript $as
            if ($r.ExitCode -eq 0 -and $r.Output.Length -gt 0) { $path = $r.Output }
        }
        'Zenity' {
            $zargs = @('--file-selection', '--save', '--confirm-overwrite', "--title=$Title")
            foreach ($f in $filters) { $zargs += ('--file-filter=' + $f.Name + ' | ' + ($f.Patterns -join ' ')) }
            $start = if ($dir) { $dir.TrimEnd('/') + '/' } else { '' }
            if ($DefaultName -or $start) { $zargs += ('--filename=' + $start + $DefaultName) }
            $r = Invoke-EJNative -File 'zenity' -Arguments $zargs
            if ($r.ExitCode -eq 0 -and $r.Output.Length -gt 0) { $path = $r.Output }
        }
        default {
            $r = @(Invoke-EJWinDialog -Dialog {
                param($Title, $Filter, $Dir, $Name)
                Add-Type -AssemblyName System.Windows.Forms
                $dialog = New-Object System.Windows.Forms.SaveFileDialog
                $dialog.Title = $Title
                $dialog.OverwritePrompt = $true
                if ($Filter) { $dialog.Filter = $Filter }
                if ($Dir) { $dialog.InitialDirectory = $Dir }
                if ($Name) { $dialog.FileName = $Name }
                if ($dialog.ShowDialog() -eq 'OK') { return $dialog.FileName }
            } -ArgumentList @($Title, (ConvertTo-EJWinFilter $filters), $dir, $DefaultName))
            if ($r.Count -gt 0) { $path = [string]$r[0] }
        }
    }

    if (-not $path) {
        Complete-EJCancel $ThrowOnCancel.IsPresent
        return ''
    }
    return $path
}

function Show-EJSelectFolderWindow {
    <#
        .SYNOPSIS
        Shows a select-folder window and returns the selected folder path.

        .PARAMETER Title
        Window title / description.

        .PARAMETER InitialDirectory
        Folder the window opens in.

        .PARAMETER ThrowOnCancel
        Throw instead of returning "" when the user cancels.

        .OUTPUTS
        The folder path, or "" when cancelled.

        .EXAMPLE
        PS> $folder = Show-EJSelectFolderWindow -Title "Where are the logs?"
    #>
    [CmdletBinding()]
    param (
        [Parameter(Position = 0)][string]$Title = 'Select a folder',
        [string]$InitialDirectory = '',
        [switch]$ThrowOnCancel
    )

    $dir = $null
    if ($InitialDirectory -and (Test-Path -LiteralPath $InitialDirectory -PathType Container)) {
        $dir = (Resolve-Path -LiteralPath $InitialDirectory).ProviderPath
    }

    $path = $null
    switch (Assert-EJBackend) {
        'AppleScript' {
            $as = 'POSIX path of (choose folder with prompt ' + (ConvertTo-EJAppleScript $Title)
            if ($dir) { $as += ' default location (POSIX file ' + (ConvertTo-EJAppleScript $dir) + ' as alias)' }
            $as += ')'
            $r = Invoke-EJOsascript $as
            if ($r.ExitCode -eq 0 -and $r.Output.Length -gt 0) { $path = $r.Output }
        }
        'Zenity' {
            $zargs = @('--file-selection', '--directory', "--title=$Title")
            if ($dir) { $zargs += ('--filename=' + $dir.TrimEnd('/') + '/') }
            $r = Invoke-EJNative -File 'zenity' -Arguments $zargs
            if ($r.ExitCode -eq 0 -and $r.Output.Length -gt 0) { $path = $r.Output }
        }
        default {
            $r = @(Invoke-EJWinDialog -Dialog {
                param($Title, $Dir)
                Add-Type -AssemblyName System.Windows.Forms
                $dialog = New-Object System.Windows.Forms.FolderBrowserDialog
                $dialog.Description = $Title
                if ($Dir) { $dialog.SelectedPath = $Dir }
                if ($dialog.ShowDialog() -eq 'OK' -and $dialog.SelectedPath) { return $dialog.SelectedPath }
            } -ArgumentList @($Title, $dir))
            if ($r.Count -gt 0) { $path = [string]$r[0] }
        }
    }

    if (-not $path) {
        Complete-EJCancel $ThrowOnCancel.IsPresent
        return ''
    }
    # macOS returns a trailing slash for folders; keep it consistent with the other platforms.
    if ($path.Length -gt 1 -and $path.EndsWith('/')) { $path = $path.TrimEnd('/') }
    return $path
}

function Show-EJNotification {
    <#
        .SYNOPSIS
        Shows a desktop notification. On macOS and Linux it returns immediately; on Windows it waits a few
        seconds so the balloon can be seen.

        .PARAMETER Title
        Notification title.

        .PARAMETER Text
        Notification text.

        .EXAMPLE
        PS> Show-EJNotification -Title "Backup" -Text "Finished in 4 minutes"
    #>
    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $true, Position = 0)][string]$Title,
        [Parameter(Mandatory = $true, Position = 1)][string]$Text
    )

    if ($script:EJIsMac) {
        [void](Invoke-EJOsascript ('display notification ' + (ConvertTo-EJAppleScript $Text) + ' with title ' + (ConvertTo-EJAppleScript $Title)))
    }
    elseif ($script:EJIsWindows) {
        Add-Type -AssemblyName System.Windows.Forms
        Add-Type -AssemblyName System.Drawing
        $icon = New-Object System.Windows.Forms.NotifyIcon
        $icon.Icon = [System.Drawing.SystemIcons]::Information
        $icon.Visible = $true
        $icon.ShowBalloonTip(5000, $Title, $Text, 'Info')
        Start-Sleep -Seconds 6
        $icon.Dispose()
    }
    elseif (Get-Command notify-send -ErrorAction SilentlyContinue) {
        [void](Invoke-EJNative -File 'notify-send' -Arguments @($Title, $Text))
    }
    elseif (Get-Command zenity -ErrorAction SilentlyContinue) {
        [void](Invoke-EJNative -File 'zenity' -Arguments @('--notification', "--text=$Title - $Text"))
    }
    else {
        throw 'No notification tool found. Install libnotify (notify-send) or zenity.'
    }
}

Export-ModuleMember -Function Show-EJInputBox, Show-EJMessageBox, Show-EJConfirm, Show-EJSelectItem, Show-EJSelectFileWindow,
    Show-EJSaveFileWindow, Show-EJSelectFolderWindow, Show-EJNotification, Get-EJDialogBackend
