[CmdletBinding()]
param(
    [switch]$Wait
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot

$services = @(
    @{
        Name = "SourceApi"
        Project = "services/SourceApi"
    },
    @{
        Name = "TargetApi"
        Project = "services/TargetApi"
    },
    @{
        Name = "IntegrationHub.Api"
        Project = "src/IntegrationHub.Api"
    },
    @{
        Name = "IntegrationHub.Web"
        Project = "src/IntegrationHub.Web"
    }
)

foreach ($service in $services) {
    $projectPath = Join-Path $root $service.Project
    $windowTitle = $service.Name
    $command = "Set-Location '$root'; `$Host.UI.RawUI.WindowTitle = '$windowTitle'; dotnet run --project '$projectPath' --launch-profile https"

    Start-Process powershell -ArgumentList @(
        "-NoExit",
        "-Command",
        $command
    )
}

if ($Wait) {
    Read-Host "Press Enter to exit"
}
