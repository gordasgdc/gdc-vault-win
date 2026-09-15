# Changelog — GDC Vault (Windows)

## v0.8.0 (2026-09-15) — Copii de siguranta automate

### Added
- **Copii de siguranta automate**, criptate, in `%AppData%/GDCVault/Backups/`:
  la fiecare pornire si inainte de fiecare salvare. Se pastreaza ultimele 10.
- **Recuperare**: daca la pornire fisierul cu aplicatiile tale lipseste sau nu
  poate fi citit, aplicatia gaseste cea mai recenta copie buna si iti ofera
  restaurarea.

## v0.7.0 (2026-09-15) — Paritate cu macOS: abonamente, linkuri, parole

### Added
- Cost optional per abonament (lunar/anual) si calcul al costului lunar.
- Reminder de reinnoire cu export in calendar (.ics), sincronizabil pe telefon.
- Generator de parole de 24 de caractere, fara caractere confundabile.
- Deschiderea linkurilor in browserul implicit.
- Copiere securizata in clipboard, cu golire automata dupa 45 de secunde.

## v0.6.6 (2026-09-14) — Contrast si teme

### Fixed
- Textul secundar se adapteaza acum la tema aleasa: pe tema deschisa nu mai
  ramane gri-pe-alb, iar pe cea inchisa se citeste clar.
- Mesajele de eroare din fereastra de activare folosesc un rosu lizibil pe
  fundal inchis, in loc de rosu saturat.
- Campurile si butoanele dezactivate raman vizibile in loc sa se estompeze
  pana la disparitie.

## v0.6.4 (2026-08-31) — Bannerul și textul de licență arătau suma veche fixă
Bannerul de sus și textul din fereastra de Activare mai arătau 5€
hardcodat, deși mesajul WhatsApp (v0.6.3) deja citea prețul dinamic din
`pricing.json` — nu erau sincronizate. Acum ambele citesc `PricingChecker`
și arată eticheta ofertei active (ex. "🔥 Ofertă lansare: Donează 9€").

## v0.6.3 (2026-08-31) — Preț dinamic din Furnizor
Suma de donație din mesajul WhatsApp de activare se citește acum din
`pricing.json` (Furnizor), nu mai e fixă în cod — orice ofertă programată
apare automat, fără recompilare.

## v0.6.2 (2026-08-30)
- **`LicenseFile` lipsea din installer** (Regula 19) — adăugat
  `installer/license.txt` + `LicenseFile=installer\license.txt` în
  `[Setup]`; Inno Setup arată acum nativ pagina "I accept the
  agreement"/"I do not accept", Next dezactivat până la acceptare.

## v0.6.1 (2026-08-29)
- **Ghidul PDF redesenat**: copertă cu banner de brand (amber), 3 capturi
  reale ale clientului WPF (fereastra principală, adăugare aplicație,
  setări), footer paginat. Fără schimbare de cod.

## v0.6.0 (2026-08-29)
- **Setare explicită "Mărime Text"** (Mic/Normal/Mare/Foarte mare, Regula 24
  — lipsea, adăugat standard după ultima actualizare a acestui repo) — în
  fereastra de Setări, alături de tema. `LayoutTransform` pe `RootGrid`,
  persistat local, aplicat instant fără repornire.

## v0.2.2 (2026-08-26)
**Completare obligatorie, Directivă Permanentă Supremă — lipsea complet:**
- **Versiune vizibilă în UI**: `v0.2.2` afișat acum în footer-ul sidebar-ului.
- **Update checker cu pop-up**: verificare automată la lansare
  (`UpdateChecker.Shared.CheckAsync`, GitHub Releases API), pop-up real
  (`Wpf.Ui.Controls.MessageBox`) dacă există o versiune nouă, cu buton
  „Descarcă". Dismissal per-versiune (fișier în `%AppData%\GDCVault\`) —
  nu reapare la fiecare pornire odată respins. Plus buton manual „Caută
  actualizări" în sidebar, care ignoră dismissal-ul (mereu arată
  rezultatul real).
- **Bonus găsit la verificare**: `installer.iss` (`0.2.0`) și
  `GDCVault.Client.csproj` (`0.1.0`) erau deja desincronizate între ele
  ÎNAINTE de acest fix — sincronizate acum la `0.2.2`, aliniat cu Mac.
