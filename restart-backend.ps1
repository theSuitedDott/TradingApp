# Stops any process on ports 5181/7065 and restarts the API.
$ErrorActionPreference = "Continue"

$dotnet = "$env:USERPROFILE\.dotnet\dotnet.exe"
if (-not (Test-Path $dotnet)) {
    $dotnet = "dotnet"
}

function Stop-PortListeners {
    param([int[]]$Ports)

    foreach ($port in $Ports) {
        $pids = @()

        try {
            $pids += Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue |
                Select-Object -ExpandProperty OwningProcess -Unique
        }
        catch { }

        # Fallback: netstat when Get-NetTCPConnection returns nothing
        if ($pids.Count -eq 0) {
            $lines = netstat -ano | Select-String ":$port\s+.*LISTENING"
            foreach ($line in $lines) {
                $pidText = ($line -split '\s+')[-1]
                if ($pidText -match '^\d+$') { $pids += [int]$pidText }
            }
        }

        foreach ($pid in ($pids | Select-Object -Unique)) {
            if ($pid -le 0) { continue }
            try {
                $proc = Get-Process -Id $pid -ErrorAction SilentlyContinue
                if ($proc) {
                    Write-Host "Stopping PID $pid ($($proc.ProcessName)) on port $port..." -ForegroundColor Yellow
                    Stop-Process -Id $pid -Force -ErrorAction SilentlyContinue
                }
            }
            catch { }
        }
    }
}

Write-Host "Stopping existing backend processes..." -ForegroundColor Yellow
Get-Process -Name "TradingApp" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Stop-PortListeners -Ports @(5181, 7065)

# Wait until ports are actually free (max 10s)
$deadline = (Get-Date).AddSeconds(10)
while ((Get-Date) -lt $deadline) {
    $stillBusy = $false
    foreach ($port in @(5181, 7065)) {
        $listener = Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue
        if ($listener) { $stillBusy = $true; break }
    }
    if (-not $stillBusy) { break }
    Start-Sleep -Milliseconds 500
}

$busyPorts = @()
foreach ($port in @(5181, 7065)) {
    if (Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue) {
        $busyPorts += $port
    }
}

if ($busyPorts.Count -gt 0) {
    Write-Host "ERROR: Ports still in use: $($busyPorts -join ', '). Run as admin or close the blocking process manually." -ForegroundColor Red
    exit 1
}

Write-Host "Starting backend on http://localhost:5181 and https://localhost:7065..." -ForegroundColor Green
Set-Location $PSScriptRoot
& $dotnet run --project "$PSScriptRoot\TradingApp\TradingApp.csproj" -c Debug
