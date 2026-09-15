using GDCVault.Core.Models;

namespace GDCVault.Client;

/// Wrapper subtire de afisare peste `VaultEntry` pentru ListView-ul din
/// sidebar.
public sealed class EntryRow
{
    public VaultEntry Entry { get; }

    public EntryRow(VaultEntry entry) => Entry = entry;

    public string Name => Entry.Name;

    /// Rezumat compact: iconite text pentru parola/serial + tipul de
    /// licentiere + zilele pana la expirare, intr-un singur rand - la fel
    /// ca VaultRow.swift (Mac).
    public string Subtitle
    {
        get
        {
            var parts = new List<string>();
            if (Entry.HasPassword) parts.Add("cont");
            if (Entry.HasSerial) parts.Add("serie");
            if (Entry.LicenseType != LicenseType.None) parts.Add(Entry.LicenseType.DisplayName());
            if (Entry.DaysUntilExpiry is int days)
            {
                parts.Add(days < 0 ? "expirat" : $"{days}z");
            }
            return parts.Count == 0 ? "—" : string.Join(" · ", parts);
        }
    }

    // ─── Insigna de expirare (v0.7.0) ───────────────────────────────────

    /// Sub 30 de zile primeste insigna cu FUNDAL, nu doar text colorat: in
    /// lista, culoarea textului singura se pierde printre randuri.
    public bool ShowsExpiryBadge => Entry.DaysUntilExpiry is int d && d <= 30;

    public string ExpiryBadgeText => Entry.DaysUntilExpiry is int d
        ? (d < 0 ? "Expirat" : $"{d} {(d == 1 ? "zi" : "zile")}")
        : "";

    /// Rosu sub 7 zile, portocaliu pana la 30. Culorile vin din tema
    /// (Regula 37) — aici doar alegem CARE resursa, nu valoarea ei.
    public string ExpiryBadgeBrushKey =>
        Entry.DaysUntilExpiry is int d && d <= 7 ? "StatusErrorBrush" : "StatusWarningBrush";

    public bool HasUsername => !string.IsNullOrWhiteSpace(Entry.Username);
    public bool HasPassword => Entry.HasPassword;
}
