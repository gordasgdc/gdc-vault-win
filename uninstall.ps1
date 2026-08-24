# GDC Vault - dezinstalare completa (Windows).
#
# REGULA PERMANENTA ecosistem GDC (2026-08-24): orice aplicatie GDC
# (existenta sau noua) trebuie sa vina cu un mecanism de dezinstalare care
# sterge ABSOLUT TOT ce a creat pe sistem - nu doar folderul din
# Program Files. Un uninstall care lasa date in AppData/Registry e
# considerat un bug.
#
# Ruleaza normal (fara admin) pentru curatarea per-user; foloseste
# -RemoveProgramFiles pentru a incerca si stergerea din Program Files
# (poate cere elevare daca a fost instalat pentru toti userii).

param(
    [switch]$RemoveProgramFiles
)

$ErrorActionPreference = "SilentlyContinue"

Write-Host "GDC Vault - dezinstalare completa" -ForegroundColor Cyan
Write-Host "=================================="

$confirm = Read-Host "Sigur vrei sa stergi GDC Vault si TOATE datele lui (licente, parole, atasamente)? [y/N]"
if ($confirm -ne "y" -and $confirm -ne "Y") {
    Write-Host "Anulat."
    exit 0
}

# 1. Aplicatia insasi - locatia standard folosita de installer.iss-ul GDC.
if ($RemoveProgramFiles) {
    $installDirs = @(
        "$env:ProgramFiles\GDC Vault",
        "${env:ProgramFiles(x86)}\GDC Vault"
    )
    foreach ($dir in $installDirs) {
        if (Test-Path $dir) {
            Remove-Item -Recurse -Force $dir
            Write-Host "Sters $dir"
        }
    }
}

# 2. Datele aplicatiei - vezi VaultMetadataStore.cs / AttachmentStore.cs
#    (%LocalAppData%\GDC Vault, inclusiv entries.json, secrets\*.bin, Attachments\).
$localAppData = Join-Path $env:LOCALAPPDATA "GDC Vault"
if (Test-Path $localAppData) {
    Remove-Item -Recurse -Force $localAppData
    Write-Host "Sters $localAppData"
}

# 3. Eventuale date roaming (%AppData%) - nefolosit inca de aplicatie,
#    dar curatat preventiv daca o versiune viitoare scrie acolo.
$roamingAppData = Join-Path $env:APPDATA "GDC Vault"
if (Test-Path $roamingAppData) {
    Remove-Item -Recurse -Force $roamingAppData
    Write-Host "Sters $roamingAppData"
}

# 4. Chei de Registry - GDC Vault nu scrie inca in Registry (nicio
#    setare persistenta acolo la data acestui scaffold), dar pastram
#    stergerea aici ca placeholder obligatoriu: daca o versiune viitoare
#    adauga o cheie (ex. HKCU\Software\GDC\Vault pentru preferinte),
#    STERGEREA EI TREBUIE ADAUGATA AICI IN ACELASI COMMIT.
$registryPath = "HKCU:\Software\GDC\Vault"
if (Test-Path $registryPath) {
    Remove-Item -Recurse -Force $registryPath
    Write-Host "Sters $registryPath"
}

Write-Host ""
Write-Host "GDC Vault a fost dezinstalat complet." -ForegroundColor Green
if (-not $RemoveProgramFiles) {
    Write-Host "NOTA: foloseste -RemoveProgramFiles ca sa stergi si folderul din Program Files." -ForegroundColor Yellow
}
