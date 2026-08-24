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
}
