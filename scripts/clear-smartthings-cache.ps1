$dataRoot = Join-Path $env:LOCALAPPDATA "SmartThingsMxConsole"
$cachePath = Join-Path $dataRoot "cache.json"

if (Test-Path $cachePath)
{
    Remove-Item -Path $cachePath -Force
    Write-Host "Removed $cachePath"
}
else
{
    Write-Host "No cache file found at $cachePath"
}
