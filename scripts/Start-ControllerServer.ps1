# Campus Pulse 2030 — Launch Mobile Controller Companion Server
param (
    [int]$Port = 8090,
    [string]$LanIp = ""
)

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Split-Path -Parent $ScriptDir
$ServerDir = Join-Path $ProjectRoot "server"

if (-not (Get-Command node -ErrorAction SilentlyContinue)) {
    Write-Error "Node.js is not found on PATH. Please install Node.js (v18+) to host the mobile controller."
    exit 1
}

Write-Host "Starting Campus Pulse 2030 Controller Server on Port $Port..." -ForegroundColor Cyan

if ($LanIp) {
    $env:CAMPUS_LAN_IP = $LanIp
}
$env:PORT = "$Port"

Set-Location $ServerDir
node server.js
