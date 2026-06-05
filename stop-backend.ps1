# Stops the backend without restarting.
$ErrorActionPreference = "Continue"

Write-Host "Stopping TradingApp..." -ForegroundColor Yellow
Get-Process -Name "TradingApp" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue

foreach ($port in @(5181, 7065)) {
    try {
        $pids = Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue |
            Select-Object -ExpandProperty OwningProcess -Unique
        foreach ($pid in $pids) {
            if ($pid -gt 0) {
                Stop-Process -Id $pid -Force -ErrorAction SilentlyContinue
                Write-Host "Freed port $port (PID $pid)."
            }
        }
    }
    catch { }
}

Write-Host "Backend stopped." -ForegroundColor Green
