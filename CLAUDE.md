# GDC Vault (Windows) — note de arhitectură

Oglinda C# a `gdc-vault-mac`. ID produs oficial: `gdc-vault`. Vezi
`gdc-vault-mac/CLAUDE.md` pentru rationamentul complet — aici doar ce diferă.

## Structură

- `src/GDCVault.Core/` — model + criptografie:
  - `Services/LicenseCore.cs` / `Services/MachineID.cs` — copiate din
    `GDCPluginManagerWin/src/GDCPluginManager.Core/Services/` (namespace ajustat
    la `GDCVault.Core.Services`). Aceeași cheie publică Ed25519 hardcodată.
  - `Models/VaultEntry.cs`, `Models/VaultEntryKind.cs`, `Models/AttachmentRef.cs`.
  - `Services/VaultDpapiStore.cs` — parole/serii criptate cu DPAPI
    (`ProtectedData.Protect(..., DataProtectionScope.CurrentUser)`), un fișier
    `.bin` per intrare în `%LocalAppData%\GDC Vault\secrets\`.
  - `Services/AttachmentStore.cs` — atașamente în
    `%LocalAppData%\GDC Vault\Attachments\<entryId>\`.
  - `Services/VaultMetadataStore.cs` — JSON simplu (fără secrete) în
    `%LocalAppData%\GDC Vault\entries.json`.
  - `Services/VaultExportImport.cs` — backup criptat AES-256-GCM, folosind
    `Rfc2898DeriveBytes.Pbkdf2` + `AesGcm` NATIVE din .NET (nicio criptografie
    proprie implementată, spre deosebire de partea Mac unde PBKDF2 e manual
    peste CryptoKit). **Parametrii (200k iterații, cheie 32B, salt 16B, nonce
    12B, tag 16B) trebuie să rămână identici cu `PBKDF2.swift`/
    `VaultExportImport.swift`** — altfel un backup exportat pe o platformă nu
    se mai importă pe cealaltă.
- `src/GDCVault.Client/` — WPF (`Wpf.Ui` 3.0.5, `CommunityToolkit.Mvvm`),
  cod-behind simplu (nu MVVM complet — e stadiul de scaffold).
- `uninstall.ps1` — dezinstalare completă (vezi Regula de Clean Uninstall).

**NOTĂ**: build-ul local (`dotnet build`) verificat doar sintaxă C#/referințe pe
Mac — compilarea XAML→BAML necesită Windows real (`PresentationBuildTasks`),
la fel ca `GDCPluginManagerWin`. Orice modificare de XAML trebuie confirmată
prin CI Windows real înainte de a fi considerată terminată.

## Regula de Clean Uninstall (permanentă, tot ecosistemul GDC)

Vezi `gdc-plugin-manager-catalog-vendor/CLAUDE.md` pentru regula completă.
Aici: `uninstall.ps1` curăță `Program Files\GDC Vault` (cu `-RemoveProgramFiles`),
`%LocalAppData%\GDC Vault` (entries.json, secrets\, Attachments\),
`%AppData%\GDC Vault` (placeholder, neutilizat încă), și
`HKCU:\Software\GDC\Vault` (placeholder — nicio cheie de Registry scrisă încă
la acest scaffold). **Dacă o versiune viitoare adaugă o cheie de Registry sau
un fișier persistent nou, adaug-o în `uninstall.ps1` în ACELAȘI commit.**

## Rebuild local (verificare sintaxă, NU echivalent cu build Windows real)

```bash
cd ~/Downloads/gdc-vault-win && dotnet build src/GDCVault.Core/GDCVault.Core.csproj && dotnet build src/GDCVault.Client/GDCVault.Client.csproj
```
