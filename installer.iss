; Instalator Windows pentru GDC Vault, cu Inno Setup
; (https://jrsoftware.org/isinfo.php — gratuit) — port 1:1 al
; installer.iss din GDCPluginManagerWin. Inlocuieste arhiva .zip
; confuza (gramada de DLL-uri fara sa fie clar ce sa rulezi) cu un
; singur executabil de instalare, care creeaza Program Files\GDC Vault,
; scurtaturi Desktop + Start Menu, si apare corect in "Apps & Features"
; cu dezinstalare curata.
;
; CI-ul (.github/workflows/build-windows.yml) face toti pasii automat.
; Pentru compilare MANUALA, pe Windows, cu Inno Setup Compiler instalat
; (gratuit, https://jrsoftware.org/isdl.php):
;   1. dotnet publish src\GDCVault.Client -c Release -r win-x64 --self-contained -o publish
;   2. Deschide acest fisier (installer.iss) cu Inno Setup Compiler
;   3. Apasa "Compile" (sau F9)
;   4. Rezultatul apare in Output\GDCVaultSetup.exe

#define MyAppName "GDC Vault"
#define MyAppVersion "0.5.4"
#define MyAppPublisher "Cristi Gordas"
#define MyAppExeName "GDCVault.exe"
#define MyAppURL "https://gordas.dev/gdc-vault"

[Setup]
AppId={{E4A9C2D1-7B3F-4E5A-9F0C-GDCVAULT00001}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\GDC\GDC Vault
DefaultGroupName=GDC Vault
DisableProgramGroupPage=yes
OutputDir=Output
OutputBaseFilename=GDCVaultSetup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
SetupIconFile=src\GDCVault.Client\Assets\app.ico
; Nu semnat cu certificat platit — Windows SmartScreen arata un
; avertisment "Unrecognized app" la prima rulare a instalatorului.
; Normal pentru distributie indie (aceeasi nota ca la GDCPluginManagerWin
; si la .pkg-ul nesemnat de pe Mac); crește prioritatea odata cu
; certificatul oficial (vezi nota de transparenta din landing page).
UninstallDisplayIcon={app}\{#MyAppExeName}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Dezinstaleaza {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#MyAppName}}"; Flags: nowait postinstall skipifsilent

; REGULA PERMANENTA de Clean Uninstall (vezi gdc-plugin-manager-catalog-vendor/CLAUDE.md,
; 2026-08-24): dezinstalarea trebuie sa stearga TOT ce a scris aplicatia,
; nu doar folderul din Program Files. %LocalAppData%\GDC Vault contine
; entries.json, secrets\*.bin (DPAPI) si Attachments\ — vezi
; VaultMetadataStore.cs/VaultDpapiStore.cs/AttachmentStore.cs. Daca o
; versiune viitoare adauga un fisier persistent nou in alta parte
; (Registry, %AppData%), adauga stergerea lui aici, in acelasi commit.
[UninstallDelete]
Type: filesandordirs; Name: "{localappdata}\GDC Vault"
