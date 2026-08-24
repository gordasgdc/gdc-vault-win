using GDCVault.Core.Models;

namespace GDCVault.Client;

/// Wrapper subtire de afisare peste `VaultEntry`, la fel ca ViewModel-urile
/// din GDCPluginManagerWin (ex. AppLinkViewModel) - ListView leaga direct
/// pe proprietati calculate (KindDisplay/ExpiryDisplay), fara sa punem
/// logica de formatare in XAML.
public sealed class EntryRow
{
    public VaultEntry Entry { get; }

    public EntryRow(VaultEntry entry) => Entry = entry;

    public string Name => Entry.Name;
    public string KindDisplay => Entry.Kind.DisplayName();

    public string ExpiryDisplay
    {
        get
        {
            var days = Entry.DaysUntilExpiry;
            if (days is null) return "—";
            return days < 0 ? "Expirat" : $"{days} zile";
        }
    }
}
