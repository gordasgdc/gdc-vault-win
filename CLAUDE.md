# GDC Vault (Windows) — note de arhitectură

Oglinda C# a `gdc-vault-mac`. ID produs oficial: `gdc-vault`. Vezi
`gdc-vault-mac/CLAUDE.md` pentru rationamentul complet — aici doar ce diferă.

## [PARTEA 1: REGULI GLOBALE ECOSISTEM GDC — identică în toate proiectele GDC]

> Acest bloc e sincronizat manual în `CLAUDE.md`-ul TUTUROR proiectelor din
> `~/Developer/` (CGConvertor, CursorPro, DataMover, GDCPluginManager,
> GDCPluginManagerWin, GDCVault, GDCVaultWin, gdc-plugin-manager-catalog-vendor,
> gdc-plugin-manager-files, gdc-production-manager, gdc-resolve-encoder, și
> orice proiect GDC nou). Dacă modifici o regulă aici, propag-o manual și în
> celelalte 10 fișiere — nu există un fișier partajat/include, fiecare
> `CLAUDE.md` e citit independent per-repo. Vezi jurnalul "Sincronizare
> CLAUDE.md" din secțiunea Partea 2 a fiecărui repo pentru data ultimei
> unificări.

**1. Directoare & structură.** Toate proiectele GDC trăiesc exclusiv în
`~/Developer/<NumeProiect>/`, niciodată în `~/Downloads` sau `~/Desktop`
(curățate automat de CleanMyMac/Hazel pe acest Mac — au șters repo-uri de
sursă în trecut). Niciun repo nou nu se creează/clonează în afara
`~/Developer/`. Certificatele Apple (`.p12`/`.cer`) și orice cheie privată
(`.p8`/`.key`/`.pem`/`.mobileprovision`) stau EXCLUSIV în
`~/Developer/Certificates/` (folder în afara oricărui repo git) — niciodată
comise, indiferent de `.gitignore`.

**2. Securitate — zero secrete în git.** `.git/config` nu conține niciodată
un token în clar în URL-ul remote-ului (`https://user:TOKEN@github.com/...`)
— autentificare exclusiv prin `gh` (credential helper) sau SSH. Orice token
găsit expus se elimină din config imediat; revocarea efectivă din GitHub
Settings e un pas manual al lui Cristi (Claude nu poate revoca un token).
Un secret comis vreodată în istoricul git (verificat cu
`git log --all -p | grep` sau echivalent) trebuie semnalat explicit, nu doar
curățat din starea curentă.

**3. Licențiere & Donație (GDC Plugin Manager / Furnizor).** Toate
aplicațiile standalone GDC folosesc `LicenseCore`/`MachineID` (Ed25519,
aceeași cheie publică hardcodată în tot ecosistemul — copiată byte-for-byte,
NU printr-o dependință de pachet între repo-uri). Probă gratuită implicită:
**15 zile**. Activare manuală prin WhatsApp (ID de mașină pre-completat) →
cod generat din `GenerateSerialView.swift` (Furnizor, `gdcStandaloneProducts`
trebuie să includă `productID`-ul noii aplicații). Valoarea susținerii
aplicației se exprimă EXCLUSIV ca **donație** — sumă implicită de referință
**23 €** dacă nu există alt preț promoțional documentat pentru acea
aplicație — NICIODATĂ cu cuvintele „preț", „cumpără" sau „vânzare" (RO/EN/ES:
niciodată „price"/„buy"/"sale" nici în engleză/spaniolă). Formularea trebuie
să apară clar în: UI-ul aplicației (ecran/pop-up de licență), ghidul PDF, și
orice pagină web dedicată.

**4. Manager de Dependențe (Standard GDC, opt-in).** Aplicația de bază
rămâne lightweight — orice dependință externă opțională/grea (ex. FFmpeg
static) se descarcă LA CERERE, nu bundle-uită implicit dacă poate fi evitat.
Indicator global 🔴/🟢 vizibil în header/meniu: verde doar dacă TOATE
componentele obligatorii (non-opționale) sunt OK; componentele opționale
(ex. Homebrew pe Mac) nu blochează starea verde. Click pe indicator deschide
un panou dedicat ("Verificare & Dependențe Sistem") cu o listă modulară de
componente (model generic `DependencyItem` — id, nume, opțional/obligatoriu,
verificare headless, acțiune, niciodată câmpuri hardcodate per-dependință),
fiecare cu propriul status + buton de acțiune (descărcare automată a unui
binar static, sau copiere comandă de instalare). Verificarea rulează headless
la fiecare deschidere a panoului/meniului, actualizând starea instant.

**5. Instalare Autonomă.** Mac: `.pkg` semnat Developer ID Application +
Installer, notarizat, stapled, cu `pkgbuild --install-location "/"` și
payload la `Applications/<App>.app` — instalare DIRECTĂ în `/Applications`
la dublu-click, fără drag-and-drop manual (verificabil cu
`pkgutil --payload-files`). Windows: installer Inno Setup cu
`DefaultDirName={autopf}\GDC\<App>` (Program Files) sau varianta x86,
scurtături automate Desktop + Start Menu, dezinstalare nativă prin
"Apps & Features" (fără script separat necesar dacă Inno Setup o acoperă).

**6. Packaging Mac — arhivă cu STRICT 3 fișiere.** Orice
`<App>-Mac.zip` livrat clientului conține la rădăcină EXACT: (1)
executabilul/`.pkg`-ul semnat+notarizat+stapled, (2)
`Dezinstalare_<App>.command` (dezinstalare completă: procese, TCC dacă
relevant, `~/Library/Application Support`, `Caches`, `Preferences`,
`Saved Application State`, `Logs`, orice item Keychain scris de aplicație),
(3) `Instructiuni_Utilizare.pdf` (RO/EN/ES). NICIODATĂ hack-uri
`xattr -dr com.apple.quarantine` sau launchere `Instalare_*.command` —
pachetul stapled e acceptat nativ de Gatekeeper. Curățarea unei instalări
vechi se face în `installer/scripts/preinstall` (`pkgbuild --scripts`,
pkill + `rm -rf`), niciodată legat de quarantine.

**7. UI Standard — varianta "Shift".** Temă dark, profesională, inspirată de
paginile de Color din DaVinci Resolve (fundal `#14161A`/`#1A1D22`, accent
cald cupru/amber sau altă culoare distinctă per-aplicație, text `#EDEFF2`).
Număr de versiune vizibil în UI (About/Meniu/Settings/Footer), fără excepție.
Update Checker automat la lansare + verificare manuală, conectat la
`update.json`/GitHub Releases API, cu notificare atât banner discrét CÂT ȘI
pop-up modal (o singură dată per versiune nouă, stare de dismissal comună
între cele două) — un simplu banner nu e suficient. `mandatory: true` în
`update.json` ignoră dismissal-ul anterior.

**8. Documentație PDF — standard ultra-detaliat.** Orice
`Instructiuni_Utilizare.pdf` (RO/EN/ES) se redactează pentru un utilizator
complet începător, zero presupuneri, cu secțiunile relevante aplicației:
(a) Panoul de Dependențe — ce înseamnă 🔴/🟢, pas-cu-pas ce face userul la
roșu (unde dă clic, ce se deschide, ce buton apasă); (b) Homebrew (Mac,
dacă aplicabil) — pași la nivel de acțiune: copiază comanda din aplicație,
deschide Terminal (Spotlight, `⌘+Space`), lipește (`⌘+V`), Enter, apoi
explică parola de Mac cerută (invizibilă la tastare) + Enter din nou;
(c) Fluxul de utilizare + acțiuni post-proces — cum se adaugă
fișiere/date, ce face fiecare buton rezultat; (d) Licență & Donație — trial
gratuit explicit (zile), suma exactă ca donație (niciodată "preț"/"vânzare").

**9. Checklist obligatoriu la FIECARE release** (păstrat identic cu
"DIRECTIVĂ PERMANENTĂ SUPREMĂ" din jurnalul fiecărui proiect — punctele
1-4 de acolo sunt subsumate integral de punctele 5-8 de mai sus). Site-ul
public al fiecărei aplicații trebuie să pointeze mereu la
`releases/latest/download/...` (HTTP 200 verificat, nu presupus), niciodată
un tag fix.

**10. Comunicare & jurnal.** Fiecare `CLAUDE.md` rămâne un jurnal
append-only (regulile vechi nu se șterg, doar se marchează
**[ÎNVECHIT]** cu motivul dacă sunt explicit invalidate). Răspunsurile
Claude rămân ultra-concise: fără explicații de proces, direct codul/
diff-ul/comenzile și statusul. La orice modificare de cod, comanda exactă
de rebuild local se include la finalul răspunsului.

**11. Sincronizare dinamică a Standardului Master (CONTINUOUS UPDATE,
2026-08-26).** Orice adăugare/modificare/optimizare a unei reguli globale
din ACEASTĂ Partea 1 — indiferent din ce proiect pornește — devine automat
noul Standard Master și TREBUIE propagată manual, în ACELAȘI commit sau
imediat următorul, în `CLAUDE.md`-ul tuturor celorlalte proiecte din
`~/Developer/` (nu doar notată "pentru mai târziu"). Orice aplicație NOUĂ
creată în `~/Developer/` primește Partea 1 (versiunea curentă, completă)
încă din primul `CLAUDE.md` scris pentru ea — nu se pornește niciodată de
la un fișier gol sau parțial. Regula 1 de mai sus ("Dacă modifici o regulă
aici, propag-o manual...") descrie mecanismul; aceasta îl declară
obligatoriu, nu opțional.

## [PARTEA 2: SPECIFICAȚII TEHNICE PROIECT]

## REGULĂ PERMANENTĂ: Locația proiectului pe disc (2026-08-26)
Acest repo trăiește în **`~/Developer/GDCVaultWin`**, NU în `~/Downloads`
(unde a stat inițial — mutat la auditul din 2026-08-26, alături de
`gdc-vault-mac` → `~/Developer/GDCVault`).

## Audit 2026-08-26
Release-ul `v0.2.0` era deja conform: 1 singur asset (`GDCVaultSetup.exe`,
Inno Setup, uninstaller nativ inclus în "Apps & Features") — nicio
modificare necesară aici, doar Mac avea probleme reale (semnare ad-hoc,
vezi `gdc-vault-mac/CLAUDE.md`).

## Completare 2026-08-26 — versiune UI + update checker (lipseau complet)
Verificat explicit (nu presupus): NICI versiunea, NICI update checker-ul
nu existau pe Windows, la fel ca pe Mac înainte de fix. Adăugat:
- `GDCVault.Core/Services/UpdateChecker.cs` (nou) — port 1:1 al
  `UpdateChecker.swift` (Mac), dar folosește direct GitHub Releases API
  (`gordasgdc/gdc-vault-win`), NU un `update.json` separat (GDC Vault nu
  are unul, spre deosebire de `gdc-plugin-manager`).
- `MainWindow.xaml`: `VersionText` (footer sidebar) + buton „Caută
  actualizări".
- `MainWindow.xaml.cs`: `MaybeShowUpdatePopupAsync` — verificare automată
  la `Loaded` (`respectDismissal: true`, nu reapare pt. o versiune deja
  închisă) + click manual (`respectDismissal: false`, mereu arată
  rezultatul real chiar dacă versiunea a fost deja închisă anterior —
  altfel butonul manual ar minți "ești la zi" pe o versiune reală, doar
  respinsă cândva). Pop-up cu `Wpf.Ui.Controls.MessageBox`.
- **Găsit pe parcurs**: `installer.iss` (`MyAppVersion=0.2.0`) și
  `GDCVault.Client.csproj` (`<Version>0.1.0</Version>`) erau ele însele
  desincronizate între ele — sincronizate acum la `0.2.2`.
- **Verificare**: `dotnet build` pe `GDCVault.Core` (Mac) — succeed, 0
  erori. XAML-ul din `Client` NU se poate compila pe Mac (vezi pitfall-ul
  identic din `gdc-plugin-manager-win/CLAUDE.md`) — validare finală prin
  CI (`build-windows.yml`), obligatorie înainte de a declara gata.

## Arhitectura fișei de produs (rescrisă 2026-08-24)

Vezi `gdc-vault-mac/CLAUDE.md` pentru rationamentul complet. Aici:
`VaultEntry` nu mai are un tip exclusiv — are `LicenseType` (informativ) +
`HasPassword`/`HasSerial` (DOUĂ fișiere DPAPI independente per intrare,
vezi `VaultDpapiStore.SecretSlot`). UI: `MainWindow` cu sidebar stânga
(butoane vizibile) + `EntryDetailControl` (UserControl, NU fereastră
modală) embedat în panoul de detaliu.

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

## Iconiță (`Assets\app.ico`)

Generat din același master 1024×1024 ca `AppIcon.icns` de pe Mac (seif
stilizat, ardezie + neon cyan/violet), via Pillow (`Image.save(..., format="ICO",
sizes=[16,32,48,64,128,256])`) — multi-rezoluție într-un singur `.ico`, așa
cum cere Windows pentru exe/title-bar la DPI-uri diferite. Conectat în
`GDCVault.Client.csproj` (`<ApplicationIcon>`) și `MainWindow.xaml`
(`Icon="pack://application:,,,/Assets/app.ico"`). **Dacă iconița se
redesenează, regenerează din același master PNG pe Mac și copiază
`app.ico` aici — nu există sursă separată pe Windows.**

## Rebuild local (verificare sintaxă, NU echivalent cu build Windows real)

```bash
cd ~/Downloads/gdc-vault-win && dotnet build src/GDCVault.Core/GDCVault.Core.csproj && dotnet build src/GDCVault.Client/GDCVault.Client.csproj
```

## CI/CD (2026-08-24)

`.github/workflows/build-windows.yml` — ruleaza pe `windows-latest` la orice
push. **Prima verificare REALA a XAML->BAML** pentru acest proiect (pana acum
doar `dotnet build` de pe Mac, care nu compileaza XAML deloc — vezi lectia
GDCPluginManagerWin, bug-ul "coperile nu se afisau" nescos la iveala de build
local). Publica un exe self-contained win-x64 ca artefact descarcabil.

## Licențiere (2026-08-24)

Oglinda Mac — `LicenseManager.cs` (`ProductId = "gdc-vault"`,
`TrialDurationDays = 15`), `Services/WhatsAppLink.cs`, `ActivationWindow`
(XAML+cs). Aceeași decizie de produs: gating DOAR pe `+ Adaugă aplicație`
(`MainWindow.xaml.cs`, `OnAddClicked`), niciodată pe vizualizare/editare/
export/import a intrărilor existente. Detalii complete în
`gdc-vault-mac/CLAUDE.md`.
