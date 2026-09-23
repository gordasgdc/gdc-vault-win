# GDC Vault (Windows) — note de arhitectură

Oglinda C# a `gdc-vault-mac`. ID produs oficial: `gdc-vault`. Vezi
`gdc-vault-mac/CLAUDE.md` pentru rationamentul complet — aici doar ce diferă.

## [PARTEA 1: REGULI GLOBALE ECOSISTEM GDC] — mutată în `~/Developer/CLAUDE.md`

> Din 2026-09-18, regulile globale stau într-un singur fișier,
> `~/Developer/CLAUDE.md`, citit automat de Claude Code în orice proiect din
> `~/Developer/`. Nu se mai copiază aici. Ce era specific acestui repo în fosta
> Partea 1 (statusuri, excepții) e la finalul fișierului.

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

## Release-țintă de test 2026-08-27 — v0.5.4, FĂRĂ schimbare funcțională
Oglindă a țintei de test `v0.5.3` din `GDCVault/CLAUDE.md` (Mac). Cristi
trebuie să instaleze o dată manual `v0.5.3` (prima versiune cu
Self-Updater), apoi să verifice din aplicație că „Actualizează acum”
descarcă+lansează installer-ul `v0.5.4` fără să deschidă browserul.

## Bug real 2026-08-27 (f) — Self-Updater real (fix-ul de link direct NU era suficient)
Oglindă a etapei (e) din `GDCVault/CLAUDE.md` (Mac). Fix-ul anterior (e)
(link direct spre asset) tot deschidea browserul — Cristi a semnalat cu
screenshot că nu s-a schimbat nimic vizibil, cerând explicit paritate cu
fluxul real deja dovedit în `GDCPluginManagerWin` (`SelfUpdater.cs`).
Portat 1:1: `SelfUpdater.cs` (nou, `GDCVault.Client`) — descarcă
`GDCVaultSetup.exe` cu `HttpClient` direct pe disc (redenumit cu versiunea,
Regula 17), apoi `Process.Start(UseShellExecute:true)` — fereastra
NATIVĂ Inno Setup apare, NICIODATĂ browserul. Nesilențios intenționat
(fără `AppMutex`/`CloseApplications` în `installer.iss`, ca la
GDCPluginManagerWin) — aplicația curentă se închide singură
(`Application.Current.Shutdown()`) înainte ca userul să ajungă la pasul
de copiere din wizard, iar `[Run] ... Flags: nowait postinstall
skipifsilent` (deja existent) relansează aplicația după instalare.
`UpdateProgressWindow.xaml(.cs)` (nou) — fereastră minimală de progres,
`Window` simplu (NU `ui:FluentWindow` — tranzitorie, fără nevoie de
`TitleBar`/drag). Butonul din popup devine „Actualizează acum”. Versiune
→ `0.5.3` (PATCH), verificat prin CI Windows real (compilare — pasul de
instalare efectiv, ca la Mac, cere confirmare manuală de la Cristi).

## Bug real 2026-08-27 (e) — "Descarcă" din popup-ul de update deschidea pagina GitHub, nu descărca
Raportat de Cristi cu screenshot: apăsând "Descarcă" în popup-ul "Este
disponibilă o versiune nouă", browserul deschidea
`github.com/.../releases/latest` (pagina web a release-ului, cu asset-urile
listate) — userul trebuia să mai caute și să apese link-ul exe-ului
manual, nu se declanșa nicio descărcare. Bug REAL, nu doar cosmetic — și
exista IDENTIC pe Mac (`UpdateChecker.swift`/`ContentView.swift`), scăpat
la fel de la implementarea inițială. Fix: `UpdateChecker.DirectDownloadUrl`
(nou) → `releases/latest/download/GDCVaultSetup.exe` (asset direct, nu
pagina) — deschiderea lui în browser DECLANȘEAZĂ descărcarea fișierului,
spre deosebire de `ReleasesPageUrl`. `MainWindow.xaml.cs` actualizat să
folosească noul link. Versiune → `0.5.2` (PATCH).

## Bug real 2026-08-27 (d) — fereastra nu se putea deplasa (raportat de Cristi)
Simptom: "fereastra rămâne fixă, nu o pot deplasa pe ecran" — primul test
real al v0.5.0 pe Windows (nu doar CI/`dotnet build`). Cauză reală:
`ui:FluentWindow` (Wpf.Ui) înlocuiește chrome-ul nativ Windows cu propriul
`WindowChrome` și NU oferă nicio zonă de drag/butoane minimize-maximize-
close fără un `<ui:TitleBar>` explicit în XAML — lipsea din TOATE cele 5
ferestre ale aplicației (`MainWindow`, `SettingsWindow`, `ActivationWindow`,
`PasswordPromptWindow`, `ProfileEditWindow`), bug PRE-EXISTENT de la
scaffold-ul inițial (2026-08-24), scos la iveală abia acum de primul test
interactiv real — `dotnet build`/CI verifică doar compilare XAML→BAML, nu
comportament runtime de drag. Fix: `<ui:TitleBar>` adăugat în toate cele 5
(rând `Auto` nou în Grid rădăcină, conținutul vechi mutat pe rândul
următor) — `MainWindow`/`SettingsWindow` cu minimize/maximize/close
complet, dialogurile mici (`ActivationWindow`/`PasswordPromptWindow`/
`ProfileEditWindow`) cu `ShowMaximize="False" ShowMinimize="False"
CanMaximize="False"` (ferestre modale de dimensiune fixă). **Regulă
practică nouă**: orice `ui:FluentWindow` nou din acest repo TREBUIE să
includă `<ui:TitleBar>` de la primul XAML scris, nu adăugat ulterior — fără
el fereastra e complet neutilizabilă (nu se poate muta/minimiza/închide
prin UI, doar Alt+F4). Versiune → `0.5.1` (PATCH), verificat prin CI
Windows real (compilare, nu comportament runtime — necesită confirmare
vizuală de la Cristi pe build-ul următor).

## Etapa 2026-08-27 (c) — Conturi multiple, Temă Light/Dark, Setări, Help PDF
Oglindă a etapei (c) din `GDCVault/CLAUDE.md` (Mac). `LoginCredential` +
`VaultEntry.AdditionalLogins` (nou, `VaultEntry.cs`), secrete DPAPI proprii
per cont (`VaultDpapiStore.*CredentialSecret`, fișier
`<entryId>.credential.<credId>.bin`, sweep la `DeleteAll`). UI:
`EntryDetailControl` — `ItemsControl`/`ObservableCollection<AdditionalLoginRow>`
sub parola principală. **Notă de paritate, NU o omisiune**: parola fiecărui
cont suplimentar e un `ui:TextBox` simplu (text vizibil), nu un
`PasswordBox` mascat ca la contul principal — binding-ul WPF pe
`PasswordBox.Password` e blocat intenționat de framework și legarea lui
într-un `ItemsControl` dinamic cere un behavior ata șat suplimentar; acceptat
ca limitare cunoscută pentru acest release, de revizuit dacă devine
relevant.

`ThemeManager.cs` (nou, `Services/`) — folosește nativ
`Wpf.Ui.Appearance.ApplicationThemeManager.Apply(...)` (pachetul deja
folosit pentru accentul amber), NU manipulare manuală de resurse; „Sistem”
citește tema Windows curentă o singură dată la selectare
(`GetSystemTheme()`), nu urmărește live schimbarea temei cât aplicația
rulează (spre deosebire de Mac, unde `NSApp.appearance = nil` urmează
dinamic sistemul) — limitare cunoscută, acceptabilă pentru acest release.
Persistat în `%LocalAppData%\GDC Vault\theme.txt`. `SettingsWindow.xaml(.cs)`
(nou) — RadioButton Sistem/Light/Dark + buton Ghid PDF, deschis din
butonul ⚙ nou din footer-ul `MainWindow` (lângă „Caută actualizări”).

**Ghid PDF — lipsea COMPLET pe Windows** (nu doar inaccesibil din UI, ca pe
Mac — nu exista deloc). Creat `installer/generate_pdf.py` (nou, oglinda
celui de pe Mac, RO/EN/ES, pași de instalare/dezinstalare adaptați la
Inno Setup/Apps & Features) → `Instructiuni_Utilizare.pdf`, inclus ca
`<Content>` în `GDCVault.Client.csproj` (copiat lângă exe la fiecare
publish — `installer.iss` îl preia automat prin `[Files] Source:
"publish\*"`, fără nicio linie nouă necesară acolo). `HelpGuide.cs` (nou)
îl deschide din `AppContext.BaseDirectory`. Versiune → `0.5.0`, verificat
prin CI Windows real.

## Etapa 2026-08-27 (b) — Bara de căutare fuzzy globală
`FuzzySearch.cs` (nou, `GDCVault.Core.Services`) — oglinda `FuzzySearch.swift`
(Mac): substring direct, apoi subsecvență de caractere în ordine, insensibil
la majuscule/diacritice (`NormalizationForm.FormD` + eliminare
`NonSpacingMark`)/spații. `VaultEntry.MatchesSearch(string)` (extension
method) caută în Nume, URL login, Notițe, Resurse și toate asset-urile
cumpărate — aceleași câmpuri ca Mac, aceeași excludere a secretelor DPAPI
reale. UI: `ui:TextBox` simplu (nu `AutoSuggestBox` — semnătura ei de
`TextChanged` e ambiguă fără sursă Wpf.Ui la îndemână, riscul exact
documentat în pitfall-ul `Symbol="Phone24"`; `ui:TextBox` extinde direct
`TextBox`, deci `TextChangedEventArgs` standard, fără presupuneri) +
glyph 🔍, deasupra listei din `MainWindow`. `Reload()` filtrează acum prin
`MatchesSearch` înainte de populare. Versiune → `0.4.0` (MINOR), verificat
prin CI Windows real.

## Etapa 2026-08-27 — Paritate cu Mac: Asset-uri, Notițe, Profil, Splitter
`VaultEntry.PurchasedAssets: List<PurchasedAsset>` (nume/cale folder/serie/
link) — proprietate nouă cu `= new()`, JSON vechi fără câmp deserializează
la listă goală, fără migrare. `EntryDetailControl` — `ItemsControl` legat
la `ObservableCollection<PurchasedAsset>`, `Microsoft.Win32.OpenFolderDialog`
(nativ .NET 8, disponibil cross-platform la compilare datorită
`EnableWindowsTargeting`) pentru selectare folder + `Process.Start` pentru
"Deschide Folder". `NotesBox` — `MinHeight=110`, scrollbar activat.
`MainWindow.xaml` — `GridSplitter` nou între sidebar (`MinWidth=220`,
`MaxWidth=480`) și panoul de detaliu (Mac are asta nativ prin
`NavigationSplitView`, Windows nu avea echivalent). Profil sidebar —
buton Copy Machine ID inline + status licență/`Activează`
(`LicenseManager.SavedLicenseCode`, nou). Versiune → `0.3.0` (MINOR),
sincronizată în `.csproj` + `installer.iss`. **Notă**: build local
(`dotnet build`, inclusiv `Client` cu XAML) a reușit curat pe Mac de data
asta (`EnableWindowsTargeting=true` permite acum compilare XAML→BAML
cross-platform pe SDK-ul curent) — confirmare finală tot prin CI Windows
real înainte de release, ca de obicei.

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

## Etapa finală (2026-08-26) — Shift UI + Profil/HWID sidebar + Revocare Licențe (Windows)
`App.xaml.cs`: `ApplicationAccentColorManager.Apply` cu amber #E8963C —
retemă completă via Wpf.Ui, fără să reimplementăm fiecare stil manual.
Port 1:1 al infrastructurii Mac: `SupabaseConfig.cs`/`AnalyticsClient.cs`/
`RevocationCheck.cs`/`UserProfileStore.cs` (Core, noi), bloc Profil în
sidebar (`MainWindow.xaml`) + `ProfileEditWindow.xaml(.cs)` (nou, port al
`PasswordPromptWindow`). Verificat prin CI real — success.

## Etapa 2026-09-11 — v0.6.5 publicat cu semnare Windows activa

Secretele CI (`WIN_SELFSIGN_PFX_BASE64`/`WIN_SELFSIGN_PFX_PASSWORD`,
certificat COMUN ecosistemului) erau deja incarcate de Cristi. Acest release
e primul in care semnarea Regulii 34 chiar a rulat pe un build real.

Verificat direct, nu presupus: pasul de semnare marcat OK in lista de pasi a
job-ului, plus directorul de securitate din header-ul PE al installer-ului
descarcat = 7496 bytes de semnatura Authenticode. Link stabil
`releases/latest/download/...` verificat HTTP 200.

Release creat manual din artefactul CI (repo fara automatizare de release).

### Completări specifice acestui repo, mutate din fosta Partea 1 (2026-09-18)

Păstrate verbatim. Regula generală la care se referă fiecare e în
`~/Developer/CLAUDE.md`.

**Regula 20:**

**Status acest repo (2026-08-27): IMPLEMENTAT.** `src/GDCVault.Client/SelfUpdater.cs` — confirmat funcțional de Cristi (v0.5.3+).

**Regula 21:**

**Status acest repo (2026-08-28, verificat): NU SE APLICA** (acelasi motiv ca varianta Mac — vezi `GDCVault`).

**Regula 34:**

  installer (asset de release sau folder `dist/`) — colaboratorii îl
  importă o SINGURĂ dată în Trusted Root, apoi orice build viitor semnat
  cu ACELAȘI certificat (persistent via secret CI, NU regenerat la
  fiecare build — un cert nou la fiecare release ar rupe încrederea deja
  acordată) e automat de încredere pe mașinile lor.
- **Aplicare**: la fiecare build de release/actualizare Windows, pe orice
  aplicație din `~/Developer/` care produce un `.exe`/installer Windows —
  aplicată incremental, la următoarea atingere reală a fiecărui repo
  (Regula 11), nu retroactiv peste tot dintr-o sesiune dedicată.
- **Implementare de referință**: CGConvertor (`build-windows.spec` +
  `.github/workflows/build-windows.yml`, 2026-09-06) — vezi
  `codesigning/README-windows.md` din acel repo pentru pașii exacți pe
  care Cristi trebuie să-i ruleze o singură dată (generare cert + upload
  secret CI). **Portat pe acest repo (GDCVaultWin, 2026-09-06)**:
  `codesigning/` (sign-windows.ps1, generate-self-signed-cert.ps1,
  README-windows.md) + `.github/workflows/build-windows.yml` (job-level
  `env.HAS_WIN_SELFSIGN` + 2 pași de semnare pentru `publish\GDCVault.exe`
  și `Output\GDCVaultSetup.exe`) — secretele `WIN_SELFSIGN_PFX_BASE64`/
  `WIN_SELFSIGN_PFX_PASSWORD` sunt ACELEAȘI (cert comun GDC) ca în
  CGConvertor, urmează să fie încărcate separat de Cristi în acest repo.

### Handoff — fișierul de stare (Regula 50, `~/Developer/CLAUDE.md`)

- Fișierul de stare al acestui proiect: `PROJECT_STATE.md` (rădăcina repo-ului). La orice sesiune nouă se citește
  ÎNTÂI el, apoi doar fragmentele strict necesare; se actualizează la milestone-uri și obligatoriu la final.
  Dacă lipsește, se creează la prima sesiune care atinge proiectul. Repo PUBLIC: fișierul e intern, listat în `.gitignore` (doar local, Regula 29).
- Restructurarea/ștergerea lui și orice modificare a acestui `CLAUDE.md`: doar cu diff-ul arătat și acordul lui Cristi.
