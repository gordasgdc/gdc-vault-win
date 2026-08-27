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

**[COMPLETARE 2026-08-26, închide o lacună de scop reală]** Interdicția de
mai sus se aplică ACUM și produselor din catalogul GDC Plugin Manager
(LUT/DCTL/PowerGrade vândute prin marketplace-ul gratuit) — găsit la audit
un card cu buton „Cumpără" și sume afișate brut („378,00 €"). Butonul
devine „Donează" peste tot (RO/EN/ES); suma documentată de furnizor pentru
acel produs (promoția specifică lui, nu neapărat 23 €) rămâne vizibilă, dar
NICIODATĂ lângă cuvântul „preț"/„cumpără"/„vânzare" — decizia anterioară de
scop (marketplace = "relație comercială diferită, nu se aplică") e
INVALIDATĂ explicit. Excepție: tabelele interne ale Furnizorului (ex.
`SalesHistoryView`, coloana „Preț" din registrul de vânzări al lui Cristi)
nu sunt UI orientat spre client — rămân neatinse.

**15. CRM Furnizor — set minim de funcționalități administrative
(2026-08-26).** Panoul de Clienți al Furnizorului (`SalesHistoryView.swift`)
nu rămâne un log rigid — trebuie să ofere: filtrare rapidă pe produs
(dropdown dinamic, nu hardcodat), export 1-click (clipboard sau fișier) al
email-urilor/HWID-urilor din selecția curentă (filtrată), copiere rapidă
per-câmp direct din tabel (fără să deschizi editarea), Licențiere în Masă
(paste o listă de email-uri/machine ID-uri → generează automat câte o
licență per linie, pentru un produs/durată alese o singură dată), și
editare liberă a duratei unei licențe deja generate (Zile/Luni/Ani/
Lifetime). Furnizorul arată versiunea curentă în UI, la fel ca orice
aplicație client — nu e scutit de Regula 7 doar pentru că e un instrument
intern.

**16. Design Web "Shift" — compact, fără spații goale (2026-08-26).**
Completare la Regula 12: paginile de prezentare NU doar adoptă paleta
amber/cupru — trebuie și dense/aerisite corect, nu găunoase. `min-height:
100svh` pe un hero cu conținut scurt lasă spațiu gol enorm pe orice ecran
mai mare — evită-l sau limitează-l (ex. `78svh`); padding-ul secțiunilor
(`section`) rămâne generos dar nu excesiv (60px, nu 90px+). Orice accent
vechi (verde/teal/albastru folosit ca accent PRIMAR, nu ca stare
semantică precum "verificat cu succes") se înlocuiește cu amber/cupru —
o variabilă CSS poate păstra alt NUME istoric (`--scope`, `--accent-copy`)
atât timp cât VALOAREA ei devine amber, ca să nu rescrii zeci de
apariții `var(--x)` din foaia de stil.

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
gratuit explicit (zile), suma exactă ca donație (niciodată "preț"/"vânzare");
(e) Cum funcționează actualizarea automată — ce înseamnă pop-up-ul de
versiune nouă, ce face butonul „Actualizează acum" vs „Mai târziu", și că
instalarea noii versiuni rămâne un pas asistat (descărcare + reinstalare),
nu un update silențios în fundal.

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

**12. Profil Utilizator/HWID în Sidebar, Sistem de Revocare Licențe &
Standard Design Web Mobile/Desktop "Shift" (2026-08-26).**
- **Profil Utilizator opțional, vizibil în sidebar-ul UI** (Mac + Windows,
  pe toate aplicațiile cu licențiere GDC): Nume (sau „Anonim" dacă nu e
  completat), Email, și Machine ID (HWID) — afișate clar, nu ascunse
  într-un submeniu. Portat din modulul Tracker existent (Mac,
  `AnalyticsClient.registerDevice` → Supabase `devices`) — Windows trebuie
  aliniat la aceeași infrastructură, nu una separată.
- **Revocare/blacklist de licențe, prin Supabase** (ACEEAȘI bază de date
  deja folosită de Tracker — niciun backend nou de construit). O licență
  Ed25519 rămâne verificată local (offline-first, nicio schimbare la
  activarea inițială), dar clientul verifică periodic + la lansare (dacă
  există conexiune) un tabel de revocări după `machineID`/serial. **Fail
  OPEN, nu fail closed**: fără conexiune la internet, o licență deja
  activată local CONTINUĂ să funcționeze (nu bricuim un user legitim offline)
  — revocarea se aplică abia la următoarea verificare online reușită.
  Furnizor capătă unelte de revocare instant + editare a perioadei de
  valabilitate a unei licențe existente deja generate.
- **Generare flexibilă de licențe** (Furnizor): selector explicit al
  duratei — Zile / Luni / Ani / Forever (Lifetime) / Valabil până la
  versiunea X — nu doar trial fix + activare permanentă binară.
- **Standard Design Web "Shift"** — orice pagină de prezentare/descărcare
  GDC (`gordas.dev` și paginile dedicate per-aplicație) adoptă design-ul
  dark, minimalist, accent amber/cupru consacrat de CG Convertor
  (`gordas.dev/cg-convertor`) — niciun accent verde vechi sau stil
  nealiniat. Toate paginile trebuie optimizate explicit pentru mobil
  (iOS Safari + Android Chrome), verificat vizual la lățimi de telefon,
  nu doar "responsive by CSS framework".

**13. Update Checker — specificație UX obligatorie (2026-08-26).** La
lansare, aplicația verifică `update.json`/GitHub Releases; dacă versiunea
locală e mai veche, arată un pop-up/modal Shift (nu doar bannerul discret
din Regula 7) cu: numărul noii versiuni, un rezumat scurt al noutăților
(Release Notes, dacă `update.json` le are — câmp opțional, degradează
elegant dacă lipsește), și DOUĂ butoane explicite — **„Actualizează acum"**
(deschide direct link-ul de descărcare a installer-ului/pachetului nou,
`releases/latest/download/...`, și arată userului că trebuie să
instaleze peste versiunea curentă + repornească aplicația — NU e un
self-update silențios, niciun helper nu înlocuiește bundle-ul/exe-ul în
fundal, vezi WARNING-ul deja existent din `UpdateChecker.swift`/`.cs`) și
**„Mai târziu"** (închide fereastra, aceeași stare de dismissal ca
bannerul). Popup-ul apare o singură dată per versiune nouă, cu excepția
`mandatory: true` (reapare la fiecare lansare). Ghidul PDF (Regula 8(e))
trebuie să explice acest flux exact.

**14. Versionare semantică obligatorie la FIECARE schimbare (2026-08-26).**
Orice modificare de cod livrată clientului — oricât de mică — incrementează
numărul de versiune, sincron în TOATE punctele care îl țin (Info.plist Mac,
`.csproj`/`installer.iss` Windows, `docs/update.json`, orice altă constantă
de versiune din acel repo). Format `MAJOR.MINOR.PATCH` (ex. `2.3.1`):
- **PATCH** (ultima cifră, `2.3.0`→`2.3.1`) — orice fix, ajustare, adăugare
  mică sau schimbare care nu rupe compatibilitatea. Cazul implicit, cel mai
  frecvent.
- **MINOR** (cifra din mijloc, `2.3.x`→`2.4.0`) — funcționalitate nouă
  vizibilă (ex. o fază/etapă întreagă ca Panoul de Dependențe sau Profilul
  HWID), fără schimbări radicale de arhitectură.
- **MAJOR** (prima cifră, `2.x.x`→`3.0.0`) — schimbare radicală: rebranding,
  redesign complet de UI, schimbare de arhitectură (ex. sistem nou de
  licențiere), sau orice prag pe care Cristi îl declară explicit "versiune
  majoră".
**De ce**: `UpdateChecker`/`.cs` compară STRICT numărul de versiune din
`update.json` cu cel instalat (`IsNewer`) — înlocuirea unui binar pe un
release existent, PE ACEEAȘI versiune, nu declanșează nicio notificare la
clienții deja instalați (bug real, găsit și reparat 2026-08-26: Windows
Shift UI + Faza 1/3/4 livrate silențios sub `v1.2.22`, fără niciun bump).
Un bump de versiune fără schimbare reală de cod e la fel de greșit ca
schimbarea de cod fără bump — cele două merg mereu împreună, în același
commit.

**17. Orice fișier descărcabil TREBUIE să poarte numărul versiunii în NUMELE
fișierului (2026-08-26).** Nu doar în interiorul aplicației (Regula 14) —
în numele fizic al pachetului: `DataMover-2.5.5.pkg`, nu `DataMover.pkg`;
`GDCPluginManagerSetup-1.2.8.exe`, nu `GDCPluginManagerSetup.exe`. Motiv
direct de la Cristi: probele/build-urile de test se acumulează local (în
`~/Downloads`, `/tmp`, trimise pentru testare) și devin de nerecunoscut
fără versiune în nume — "am o grămadă de descărcări și nu știu ce versiune
sunt, care, ce și cum sunt".
- **Excepție, NU o contrazicere**: mecanismul `releases/latest/download/
  <nume-stabil>` (site-ul, self-updater-ul) are nevoie STRUCTURAL de un
  nume care nu se schimbă niciodată între release-uri — vezi Regula
  Domeniului & Download. Copia asta stabilă (`DataMover.pkg`,
  `GDCPluginManager.pkg`) tot trebuie publicată, DAR ALĂTURI de copia
  versionată, niciodată singură. `build_installer.sh`/`build_app.sh` din
  fiecare repo produc deja ambele — regula asta cere doar ca ambele să
  ajungă mereu pe release, nu doar cea stabilă.
- **Orice fișier construit/descărcat/trimis lui Cristi în afara acestui
  mecanism** (build local de test, artefact de CI descărcat manual,
  fișier trimis prin `SendUserFile`, copie pusă în `/tmp` pentru
  verificare) TREBUIE redenumit explicit cu versiunea înainte de a fi
  oferit — niciodată livrat cu numele generic/stabil, care are sens doar
  ca țintă a unui link fix, nu ca fișier de sine stătător pe disc.

**18. Standard UX/Arhitectură obligatoriu pentru orice aplicație desktop
NOUĂ, de la primul release (2026-08-26).** Stabilit după MediaFlow Monitor
v1.3.0 — patru cerințe care nu mai sunt opționale pentru nicio aplicație
GDC viitoare (Mac și, unde tehnologia o permite, Windows):
- **Mutare automată în `/Applications` (Mac)** — la lansare, dacă bundle-ul
  rulează în afara `/Applications` sau `~/Applications` (tipic: extras
  direct din `.zip`/Downloads, sub App Translocation), aplicația arată un
  prompt nativ ("Doriți să mutați X în Aplicații?") și, la confirmare,
  copiază bundle-ul, relansează din noua locație și mută originalul la
  Coșul de gunoi. Vezi implementarea de referință `AppMover.swift`
  (MediaFlow Monitor) — fără dependință externă (PFMoveToApplicationsFolder
  nu are un port SPM întreținut), doar `NSAlert` + `FileManager`.
- **Fereastră principală redimensionabilă liber**, cu o dimensiune minimă
  de siguranță (`minSize`/`minWidth`+`minHeight`) sub care conținutul nu
  mai e lizibil — nu ferestre cu dimensiune fixă hardcodată.
- **Selector explicit de temă System/Dark/Light**, independent de setarea
  macOS/Windows — unii clienți vor Light chiar și noaptea, alții Dark
  permanent; NU e suficient să urmezi orbește `prefers-color-scheme`/tema
  sistemului. Persistat local (`UserDefaults`/Registry), aplicat imediat
  fără repornire. Vezi `AppTheme.swift`/`ThemeManager` (MediaFlow Monitor).
- **Protocolul de semnare, notarizare, auto-update și integrare GDC
  Manager rămâne cel deja documentat în Regulile 3, 5, 6, 13, 14, 17** —
  regula asta nu introduce un protocol nou, doar reconfirmă că orice
  aplicație nouă îl respectă de la prima versiune publicată, nu "adăugat
  ulterior quando there's time".

**19. Regulă Legală & Packaging (UE/Global) (2026-08-27).**
- **Pagini Web.** Orice landing page nouă sau actualizare de site publicată
  pe `gordas.dev` (sau pe orice site GDC, inclusiv paginile de proiect
  `gordasgdc.github.io/<repo>`) TREBUIE să conțină în footer link-uri către
  `https://gordas.dev/termeni` (Termeni și Condiții),
  `https://gordas.dev/confidentialitate` (Politică de Confidențialitate
  GDPR) și, unde e relevant, `https://gordas.dev/cookie` (Cookie-uri),
  plus o notă scurtă de statut: *"gordas.dev este o platformă administrată
  de dezvoltatori independenți. Aplicațiile și resursele sunt furnizate ca
  atare (AS IS), iar susținerea proiectului se bazează pe contribuții
  opționale de sprijin și donații."* Sursa canonică a acestor 3 pagini
  legale trăiește în `gdc-plugin-manager-catalog-vendor/docs/` — orice alt
  site GDC linkuiește către ele (absolut), nu le duplică.
- **Installere (.pkg macOS / .exe Windows).** Începând cu următoarele
  versiuni/build-uri (NU retroactiv — fără rebuild al aplicațiilor deja
  publicate doar pentru asta), scripturile de instalare
  (`build_installer.sh`/`productbuild` pe Mac, `installer.iss`/Inno Setup
  pe Windows) TREBUIE să includă un pas de acceptare a licenței (License
  Agreement/SLA), bazat pe un fișier `license.rtf`/`license.txt` cu un
  extras din Termeni și Condiții (statut de proiect independent,
  licențiere legată de Machine ID, natura de donație a susținerii,
  limitarea răspunderii "as is"). Utilizatorul trebuie să apese explicit
  "Agree"/"I accept" înainte ca instalarea să se finalizeze.

  **[COMPLETARE 2026-08-27] Consimțământ obligatoriu (Consent Gate), nu
  doar text afișat.** Nu e suficient ca licența să apară — pasul trebuie
  să blocheze efectiv avansarea fără acceptare explicită:
  - **macOS (`productbuild`/Distribution.xml).** Elementul `<license
    file="License.txt" mime-type="text/plain"/>` din `Distribution.xml`
    (deja folosit de `build_installer.sh` în `gdc-plugin-manager-catalog-vendor`
    și `gdc-vault-mac`) e SUFICIENT — pagina nativă de licență a
    installer-ului macOS oferă mereu doar "Agree"/"Disagree", iar
    "Continue" nu apare fără "Agree" apăsat; nu există flag care s-o
    ocolească. Regula practică: orice `Distribution.xml` nou generat
    TREBUIE să păstreze elementul `<license>` — omiterea lui (ex. un
    installer simplificat fără pas de licență) NU e acceptabilă.
  - **Windows (Inno Setup).** Secțiunea `[Setup]` din `installer.iss`
    TREBUIE să seteze `LicenseFile=license.txt` (sau `.rtf`) — Inno Setup
    arată atunci nativ o pagină cu opțiunile radio "I accept the
    agreement" / "I do not accept", cu butonul "Next" dezactivat până la
    alegerea explicită "I accept". (Dacă vreun installer Windows ar trece
    vreodată pe NSIS în loc de Inno Setup, echivalentul e
    `!insertmacro MUI_PAGE_LICENSE` cu `MUI_LICENSEPAGE_CHECKBOX` definit,
    pentru varianta cu bifă explicită.)
  - Fișierul `license.txt`/`.rtf` folosit la acest pas trebuie să conțină
    (măcar rezumat) cele 4 puncte cheie din Termeni: statut independent
    (non-comercial), licențiere Machine ID, natura de donație a
    susținerii, garanție "as is"/limitarea răspunderii — nu doar un MIT
    License generic.

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
