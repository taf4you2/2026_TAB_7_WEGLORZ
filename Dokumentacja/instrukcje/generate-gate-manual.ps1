[CmdletBinding()]
param(
    [string]$OutputPath
)

$ErrorActionPreference = 'Stop'
$scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
$sourcePath = Join-Path $scriptDirectory 'bramka\instrukcja-bramki.html'
$outputDirectory = Join-Path $scriptDirectory 'output'

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $outputDirectory 'Instrukcja_bramki.pdf'
}

$sourcePath = [System.IO.Path]::GetFullPath($sourcePath)
$OutputPath = [System.IO.Path]::GetFullPath($OutputPath)

if (-not (Test-Path -LiteralPath $sourcePath)) {
    throw "Nie znaleziono pliku zrodlowego: $sourcePath"
}

$browserCandidates = @(
    'C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe',
    'C:\Program Files\Microsoft\Edge\Application\msedge.exe',
    'C:\Program Files\Google\Chrome\Application\chrome.exe',
    'C:\Program Files (x86)\Google\Chrome\Application\chrome.exe'
)

$browserPath = $browserCandidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
if (-not $browserPath) {
    throw 'Nie znaleziono Microsoft Edge ani Google Chrome. Zainstaluj jedna z tych przegladarek.'
}

$resolvedOutputDirectory = Split-Path -Parent $OutputPath
New-Item -ItemType Directory -Path $resolvedOutputDirectory -Force | Out-Null

$sourceUri = [System.Uri]::new($sourcePath).AbsoluteUri
$arguments = @(
    '--headless=new',
    '--disable-gpu',
    '--disable-extensions',
    '--no-pdf-header-footer',
    "--print-to-pdf=$OutputPath",
    $sourceUri
)

$process = Start-Process -FilePath $browserPath -ArgumentList $arguments -Wait -PassThru -WindowStyle Hidden
if ($process.ExitCode -ne 0) {
    throw "Generowanie PDF zakonczylo sie kodem $($process.ExitCode)."
}

if (-not (Test-Path -LiteralPath $OutputPath)) {
    throw "Przegladarka nie utworzyla pliku PDF: $OutputPath"
}

$pdf = Get-Item -LiteralPath $OutputPath
Write-Output "Wygenerowano: $($pdf.FullName)"
Write-Output "Rozmiar: $($pdf.Length) bajtow"
