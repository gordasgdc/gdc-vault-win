using System.Windows;
using System.Windows.Input;
using GDCVault.Core.Services;

namespace GDCVault.Client;

public partial class MainWindow
{
    private readonly VaultMetadataStore _store = new();

    public MainWindow()
    {
        InitializeComponent();
        Reload();
    }

    private void Reload()
    {
        EntriesList.ItemsSource = _store.Entries.Select(e => new EntryRow(e)).ToList();

        var expiring = _store.ExpiringSoon();
        if (expiring.Count == 0)
        {
            ExpiringBanner.Visibility = Visibility.Collapsed;
        }
        else
        {
            ExpiringBanner.Visibility = Visibility.Visible;
            ExpiringBannerText.Text = "Expiră curând: " + string.Join(", ",
                expiring.Select(e => $"{e.Name} ({(e.DaysUntilExpiry < 0 ? "expirat" : e.DaysUntilExpiry + " zile")})"));
        }
    }

    private void OnAddClicked(object sender, RoutedEventArgs e)
    {
        var editor = new EntryEditorWindow(_store, null) { Owner = this };
        editor.ShowDialog();
        Reload();
    }

    private void OnEntryDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (EntriesList.SelectedItem is not EntryRow row) return;
        var editor = new EntryEditorWindow(_store, row.Entry) { Owner = this };
        editor.ShowDialog();
        Reload();
    }
}
