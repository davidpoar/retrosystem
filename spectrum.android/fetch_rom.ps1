#!/usr/bin/env pwsh
# fetch_rom.ps1
# Descarga la ROM 48K del ZX Spectrum desde rustzx (MIT, con permiso de Amstrad)
# y la coloca en la carpeta de assets para empaquetar en el APK.
#
# Fuentes alternativas (mismo binario, todos con permiso de Amstrad):
#   - https://raw.githubusercontent.com/rustzx/rustzx/master/rustzx-core/src/zx/roms/48.rom
#   - https://raw.githubusercontent.com/fuse-emulator/fuse/master/fuse/peripherals/spectrum/roms/48.rom
#
# La ROM sigue siendo copyright (C) 1982 Sinclair Research Ltd (ahora Amstrad plc).
# Amstrad ha dado permiso para distribuirla junto con emuladores.

[CmdletBinding()]
param(
    [string]$DestinationDir,
    [string]$RomFileName = "48.rom"
)

if (-not $DestinationDir) {
    $DestinationDir = Join-Path $PSScriptRoot "Assets/spectrum"
}

$ErrorActionPreference = "Stop"

$sources = @(
    "https://raw.githubusercontent.com/rustzx/rustzx/master/rustzx-core/src/zx/roms/48.rom",
    "https://raw.githubusercontent.com/fuse-emulator/fuse/master/fuse/peripherals/spectrum/roms/48.rom"
)

if (-not (Test-Path $DestinationDir)) {
    New-Item -ItemType Directory -Path $DestinationDir -Force | Out-Null
}

$dest = Join-Path $DestinationDir $RomFileName

if (Test-Path $dest) {
    $existingSize = (Get-Item $dest).Length
    if ($existingSize -eq 16384) {
        Write-Host "[fetch_rom] ROM ya presente y válida: $dest ($existingSize bytes)" -ForegroundColor Green
        return
    }
    Write-Host "[fetch_rom] ROM presente pero tamaño inesperado ($existingSize bytes), re-descargando..." -ForegroundColor Yellow
}

$tempFile = [System.IO.Path]::GetTempFileName()
$downloaded = $false

foreach ($url in $sources) {
    Write-Host "[fetch_rom] Intentando: $url" -ForegroundColor Cyan
    try {
        Invoke-WebRequest -Uri $url -OutFile $tempFile -UseBasicParsing -TimeoutSec 30
        $size = (Get-Item $tempFile).Length
        if ($size -eq 16384) {
            Write-Host "[fetch_rom] Descargado OK: $size bytes" -ForegroundColor Green
            $downloaded = $true
            break
        }
        Write-Host "[fetch_rom] Tamaño inesperado: $size bytes, probando siguiente fuente..." -ForegroundColor Yellow
    }
    catch {
        Write-Host "[fetch_rom] Error: $($_.Exception.Message)" -ForegroundColor Red
    }
}

if (-not $downloaded) {
    Remove-Item $tempFile -ErrorAction SilentlyContinue
    Write-Host ""
    Write-Host "[fetch_rom] ============================================================" -ForegroundColor Red
    Write-Host "[fetch_rom] No se pudo descargar la ROM automáticamente." -ForegroundColor Red
    Write-Host "[fetch_rom] Por favor, coloca manualmente el archivo '48.rom' (16 KB) en:" -ForegroundColor Red
    Write-Host "[fetch_rom]   $dest" -ForegroundColor Red
    Write-Host "[fetch_rom] Fuentes para obtenerlo:" -ForegroundColor Red
    Write-Host "[fetch_rom]   - https://github.com/rustzx/rustzx/blob/master/rustzx-core/src/zx/roms/48.rom" -ForegroundColor Red
    Write-Host "[fetch_rom]   - https://worldofspectrum.org/roms/48k" -ForegroundColor Red
    Write-Host "[fetch_rom] ============================================================" -ForegroundColor Red
    exit 1
}

Move-Item $tempFile $dest -Force
Write-Host "[fetch_rom] ROM guardada en: $dest" -ForegroundColor Green
Write-Host "[fetch_rom] (c) 1982 Sinclair Research Ltd. (Amstrad plc) - distribuida con permiso."
