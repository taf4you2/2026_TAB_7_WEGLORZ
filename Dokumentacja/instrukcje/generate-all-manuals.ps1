[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path

Write-Output 'Generowanie instrukcji panelu administratora...'
& node (Join-Path $scriptDirectory 'generate-admin-manual.mjs')
if ($LASTEXITCODE -ne 0) {
    throw "Generator instrukcji administratora zakonczyl sie kodem $LASTEXITCODE."
}

Write-Output 'Generowanie instrukcji bramki...'
& (Join-Path $scriptDirectory 'generate-gate-manual.ps1')

Write-Output 'Gotowe. Pliki PDF znajduja sie w katalogu Dokumentacja\instrukcje\output.'
