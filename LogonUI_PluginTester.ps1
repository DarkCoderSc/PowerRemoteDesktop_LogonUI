<#-------------------------------------------------------------------------------
    Power Remote Desktop LogonUI Tester
 
    .Developer
        Jean-Pierre LESUEUR (@DarkCoderSc)
        https://www.twitter.com/darkcodersc
        https://github.com/DarkCoderSc
        www.phrozen.io
        jplesueur@phrozen.io
        PHROZEN
    .License
        Apache License
        Version 2.0, January 2004
        http://www.apache.org/licenses/
    
    .Description
        This is not a script to use in a production environment.
        This is ment to be used during the development of PowerRemoteDesktop_LogonUI C# Plugin.                
-------------------------------------------------------------------------------#>

Add-Type -Assembly System.Windows.Forms
Add-Type -Assembly System.Drawing
Add-Type -MemberDefinition '[DllImport("User32.dll")] public static extern bool SetProcessDPIAware();' -Name User32 -Namespace W

$global:LogonUITaskName = "PowerRemoteDesktop_LogonUI"
$global:LogonUISnapshotDirectory = ".\LogonUI_Snapshots\"

$null = [W.User32]::SetProcessDPIAware()

function Install-WinLogonPlugin
{
    <#
        .SYNOPSIS
            Create a new Microsoft Windows Task as NT AUTHORITY\SYSTEM. The LogonUI Plugin must be run as System User.
            This trick avoid the need of external tools like PSExec to run an application as System.

        .PARAMETER PluginPath
            Type: FilePath
            Default: None
            Description: LogonUI Plugin File Path.

    #>
    param (
        [Parameter(Mandatory=$True)]
        [System.IO.FileInfo] $PluginPath
    )    

    if (-not (Get-ScheduledTask | Where-Object {$_.TaskName -like $global:LogonUITaskName }))
    {            
        $taskDescription = "Run Power Remote Desktop LogonUI Plugin"

        $action = New-ScheduledTaskAction -Execute $PluginPath

        $null = Register-ScheduledTask -Force -Action $action -TaskName $global:LogonUITaskName -Description $taskDescription -User "NT AUTHORITY\SYSTEM"
    }    

    Stop-ScheduledTask -TaskName $global:LogonUITaskName
    Start-ScheduledTask $global:LogonUITaskName
}

function Uninstall-WinLogonPlugin
{
    <#
        .SYNOPSIS
            Remove WinLogon Plugin Task from Microsoft Task Scheduler.
    #>
    Stop-Process -Force -Name "PowerRemoteDesktop_LogonUI" -ErrorAction 'silentlycontinue'

    Unregister-ScheduledTask -TaskName $global:LogonUITaskName -Confirm:$false
}

function Invoke-WinLogonSnapshot
{
    <#
        .SYNOPSIS
            Attempt to connect to open a named pipe connection with an existing LogonUI Instance.
            If visible and available, attempt to catch WinLogon Desktop Snapshot.
    #>
    $logonUIData = New-Object -TypeName PSCustomObject -Property @{
        Success = $false
        Image = $null       
    }    
    try
    {
        $pipeClient = New-Object System.IO.Pipes.NamedPipeClientStream(".", "PowerRemoteDesktop_LogonUI", [System.IO.Pipes.PipeDirection]::In)

        $pipeClient.Connect()

        $reader = New-Object System.IO.StreamReader($pipeClient)
   

        switch($reader.ReadLine())
        {
            "STREAM"
            {                
                $stream = New-Object System.IO.MemoryStream
                try
                {
                    [byte[]] $buffer = [System.Convert]::FromBase64String(($reader.ReadLine()))
                  
                    $stream.Write($buffer, 0, $buffer.Length)                  
                  
                    $stream.position = 0                                    

                    $logonUIData.Success = $true  
                    $logonUIData.Image = $stream
                }
                catch
                {
                    if ($stream)
                    {
                        $stream.Close()
                    }
                }
                finally
                {  }
            }

            "NULL"
            {                   
                $logonUIData.Success = $true
            }            
        }
    }
    catch
    { 
        if ($bitmap)
        {
            $bitmap.Dispose()
        }
    }
    finally
    {
        if ($reader)
        {
            $reader.Dispose()
        }

        if ($pipeClient)
        {
            $pipeClient.Dispose()
        }        
    }

    return $logonUIData
}

function Test-Administrator
{
    <#
        .SYNOPSIS
            Return true if current PowerShell is running with Administrator privilege, otherwise return false.
    #>
    $windowsPrincipal = New-Object Security.Principal.WindowsPrincipal(
        [Security.Principal.WindowsIdentity]::GetCurrent()
    )
    
    return $windowsPrincipal.IsInRole(
        [Security.Principal.WindowsBuiltInRole]::Administrator
    )    
}


function Invoke-WinLogonPlugin
{
    <#
        .SYNOPSIS
            Install LogonUI Plugin and wait for WinLogon Desktop Snapshot.

         .PARAMETER PluginPath
            Type: FilePath
            Default: None
            Description: LogonUI Plugin File Path.

    #>
    param (
        [Parameter(Mandatory=$True)]
        [System.IO.FileInfo] $PluginPath
    )   
    #>
    if (-not (Test-Administrator))
    {
        throw "You must run this function as administrator."
    }

    $PluginPath = (Resolve-Path -Path $PluginPath).Path    
   
    Install-WinLogonPlugin -PluginPath $PluginPath
    try
    {        
        while ($true)
        {    
            Measure-Command {
                $logonUIData = Invoke-WinLogonSnapshot
            }

            if ($logonUIData.Success -eq $true -and $logonUIData.Image)
            {
                $fileName = (Get-Date -format 'yyyy-MM-dd_HH-mm-ss-ffff')

                $image = [System.Drawing.Image]::FromStream($logonUIData.Image)

                $image.Save("$((Resolve-Path -Path $global:LogonUISnapshotDirectory).Path)$fileName.png", [System.Drawing.Imaging.ImageFormat]::Png)

                $logonUIData.Image.Close()
            }

            Start-Sleep -Seconds 1
        }
    }
    finally
    {
        Uninstall-WinLogonPlugin
    }
}

# Entry Point.
# Modify bellow options only if you are sure of what you are doing.
# Everything is relative but the PowerShell instance working directory that will run this script must
# be set to project root directory.

$ErrorActionPreference = "stop"

if ($null -eq (Get-ChildItem | Where-Object -FilterScript { $_.Name -eq "PowerRemoteDesktop_LogonUI.sln" }))
{
    throw "Error: Your PowerShell Terminal Working Directory must be set to PowerRemoteDesktop_LogonUI project root directory."
}

$null = New-Item -Path $global:LogonUISnapshotDirectory -ItemType Directory -ErrorAction "SilentlyContinue"

Get-ChildItem -Path $global:LogonUISnapshotDirectory -Include "*.png" -File -Recurse | ForEach-Object { $_.Delete() }
 
Invoke-WinLogonPlugin -PluginPath ".\PowerRemoteDesktop_LogonUI\bin\Debug\PowerRemoteDesktop_LogonUI.exe"