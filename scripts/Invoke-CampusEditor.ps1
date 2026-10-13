param(
    [ValidateSet('foundation','benchmark','campus','edit-tests','play-tests','build','refresh')]
    [string]$Command = 'edit-tests'
)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$queuePath = Join-Path $projectRoot 'Library/CampusAutomation'
New-Item -ItemType Directory -Force -Path $queuePath | Out-Null
$requestPath = Join-Path $queuePath 'request.txt'
if (Test-Path -LiteralPath $requestPath) { throw 'An editor request is already pending.' }
Set-Content -LiteralPath $requestPath -Value $Command
Write-Output "Queued $Command. The open Unity editor must be imported and responsive. Results: Logs/Campus/automation-result.txt"
