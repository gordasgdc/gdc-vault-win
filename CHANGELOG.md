# Changelog — GDC Vault (Windows)

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
