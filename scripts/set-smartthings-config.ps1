[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$PersonalAccessToken,

    [string]$RefreshInterval = "00:00:15",

    [switch]$PollAllDevices
)

$dataRoot = Join-Path $env:LOCALAPPDATA "SmartThingsMxConsole"
$settingsPath = Join-Path $dataRoot "settings.json"
$secretPath = Join-Path $dataRoot "secrets.json"

New-Item -ItemType Directory -Force -Path $dataRoot | Out-Null

$settings = @{
    providerName = "PAT"
    refreshInterval = $RefreshInterval
    pollAssignedDevicesOnly = -not $PollAllDevices.IsPresent
    favorites = @()
}

$settings | ConvertTo-Json -Depth 6 | Set-Content -Path $settingsPath -Encoding UTF8

$payload = @{
    "smartthings-pat" = $PersonalAccessToken
}
$payload | ConvertTo-Json -Depth 3 | Set-Content -Path $secretPath -Encoding UTF8

Write-Host "Wrote settings to $settingsPath"
Write-Host "Stored PAT in $secretPath"
