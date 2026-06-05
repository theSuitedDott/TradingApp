<#
.SYNOPSIS
  Development setup script: installs mkcert (via Chocolatey if available), generates localhost certs,
  creates a PFX for Kestrel and mounts location used by docker-compose, and optionally starts docker compose.

  Usage:
    .\setup-dev.ps1            # run generation only
    .\setup-dev.ps1 -Docker   # also runs `docker compose up --build`

  Requirements (Windows dev flow):
    - PowerShell (Windows) with execution allowed for local scripts.
    - Optional: Chocolatey (if missing, the script will instruct you).
#>

param(
    [switch]$Docker
)
Write-Host "Running development setup (mkcert)..." -ForegroundColor Cyan

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Push-Location $root

$certDir = Join-Path $root 'certs'
if (-not (Test-Path $certDir)) {
    New-Item -ItemType Directory -Path $certDir | Out-Null
}

function Ensure-Command($name, [string]$chocoPackage = $null) {
    if (Get-Command $name -ErrorAction SilentlyContinue) {
        return $true
    }
    if ($chocoPackage -and (Get-Command choco -ErrorAction SilentlyContinue)) {
        Write-Host "$name not found — installing via Chocolatey: $chocoPackage" -ForegroundColor Yellow
        & choco install $chocoPackage -y --no-progress
        return (Get-Command $name -ErrorAction SilentlyContinue) -ne $null
    }
    return $false
}

# Ensure mkcert (install via choco if available)
if (-not (Ensure-Command 'mkcert' 'mkcert')) {
    Write-Host "mkcert is not installed and Chocolatey was not available to install it." -ForegroundColor Red
    Write-Host "Please install mkcert manually: https://github.com/FiloSottile/mkcert" -ForegroundColor Yellow
    Exit 1
}

Write-Host "Installing mkcert root CA (mkcert -install)..." -ForegroundColor Green
& mkcert -install

$certPem = Join-Path $certDir 'localhost.pem'
$keyPem  = Join-Path $certDir 'localhost-key.pem'

Write-Host "Generating certificate for localhost, 127.0.0.1 and ::1..." -ForegroundColor Green
& mkcert -cert-file $certPem -key-file $keyPem localhost 127.0.0.1 ::1

if (-not (Test-Path $certPem) -or -not (Test-Path $keyPem)) {
    Write-Host "Failed to generate mkcert files." -ForegroundColor Red
    Exit 1
}

# Ensure openssl is available (for generating PFX)
if (-not (Ensure-Command 'openssl' 'openssl.light')) {
    Write-Host "OpenSSL was not available and could not be installed automatically." -ForegroundColor Yellow
    Write-Host "If you have Git for Windows, OpenSSL may already be available in the Git Bash environment." -ForegroundColor Yellow
    Write-Host "You can also install OpenSSL with Chocolatey: choco install openssl.light" -ForegroundColor Yellow
    Exit 1
}

$pfxPath = Join-Path $certDir 'localhost.pfx'
$pfxPassword = 'changeit'

Write-Host "Creating PFX ($pfxPath) ..." -ForegroundColor Green
& openssl pkcs12 -export -out $pfxPath -inkey $keyPem -in $certPem -passout pass:$pfxPassword

if (-not (Test-Path $pfxPath)) {
    Write-Host "Failed to create PFX." -ForegroundColor Red
    Exit 1
}

Write-Host "Certificates generated in: $certDir" -ForegroundColor Green
Write-Host "PFX password: $pfxPassword" -ForegroundColor Yellow

if ($Docker) {
    if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
        Write-Host "Docker not found in PATH. Please install Docker Desktop and rerun the script with -Docker." -ForegroundColor Red
        Exit 1
    }
    Write-Host "Starting docker compose (build)..." -ForegroundColor Green
    & docker compose up --build
}

Pop-Location
Write-Host "Done." -ForegroundColor Cyan
