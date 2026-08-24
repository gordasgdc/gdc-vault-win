using System.Windows;
using GDCVault.Core.Models;
using GDCVault.Core.Services;
using Microsoft.Win32;

namespace GDCVault.Client;

public partial class MainWindow
{
    private readonly VaultMetadataStore _store = new();
    private VaultEntry? _draftEntry; // non-null cat timp se completeaza o intrare noua, inca nesalvata

    public MainWindow()
    {
        InitializeComponent();
        Reload();
        ShowEmptyState();
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

    private void ShowEmptyState()
    {
        DetailHost.Content = null;
        EmptyStateText.Visibility = Visibility.Visible;
    }

    private void ShowDetail(VaultEntry entry, bool isNew)
    {
        EmptyStateText.Visibility = Visibility.Collapsed;
        var detail = new EntryDetailControl(_store, entry, isNew);
        detail.Saved += saved =>
        {
            _draftEntry = null;
            Reload();
            SelectRow(saved.Id);
        };
        detail.Deleted += () =>
        {
            _draftEntry = null;
            Reload();
            ShowEmptyState();
        };
        detail.CanceledNew += () =>
        {
            _draftEntry = null;
            ShowEmptyState();
            EntriesList.SelectedItem = null;
        };
        DetailHost.Content = detail;
    }

    private void SelectRow(Guid entryId)
    {
        var row = (EntriesList.ItemsSource as IEnumerable<EntryRow>)?.FirstOrDefault(r => r.Entry.Id == entryId);
        EntriesList.SelectedItem = row;
    }

    private void OnAddClicked(object sender, RoutedEventArgs e)
    {
        _draftEntry = new VaultEntry { Name = "" };
        EntriesList.SelectedItem = null;
        ShowDetail(_draftEntry, isNew: true);
    }

    private void OnSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (EntriesList.SelectedItem is not EntryRow row) return;
        _draftEntry = null;
        ShowDetail(row.Entry, isNew: false);
    }

    private void OnExportClicked(object sender, RoutedEventArgs e)
    {
        var password = PasswordPromptWindow.Ask(this, "Parolă Master de export",
            "Backup-ul (licențe, notițe, parole, atașamente) va fi criptat AES-256 cu această parolă. Reține-o — fără ea, backup-ul nu poate fi restaurat.");
        if (string.IsNullOrEmpty(password)) return;

        var dialog = new SaveFileDialog
        {
            FileName = "gdc-vault-backup.gdcvault",
            Filter = "GDC Vault backup (*.gdcvault)|*.gdcvault"
        };
        if (dialog.ShowDialog() != true) return;

        try
        {
            VaultExportImport.Export(dialog.FileName, password, _store.Entries);
        }
        catch (Exception ex)
        {
            new Wpf.Ui.Controls.MessageBox { Title = "Export eșuat", Content = ex.Message }.ShowDialogAsync();
        }
    }

    private void OnImportClicked(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "GDC Vault backup (*.gdcvault)|*.gdcvault|Toate fișierele (*.*)|*.*"
        };
        if (dialog.ShowDialog() != true) return;

        var password = PasswordPromptWindow.Ask(this, "Parolă Master de import",
            "Introdu parola cu care a fost criptat acest backup.");
        if (string.IsNullOrEmpty(password)) return;

        try
        {
            VaultExportImport.ImportBundle(dialog.FileName, password, _store);
            Reload();
        }
        catch (VaultExportImport.WrongPasswordException)
        {
            new Wpf.Ui.Controls.MessageBox { Title = "Import eșuat", Content = "Parolă greșită sau fișier corupt — nu s-a putut decripta backup-ul." }.ShowDialogAsync();
        }
        catch (Exception ex)
        {
            new Wpf.Ui.Controls.MessageBox { Title = "Import eșuat", Content = ex.Message }.ShowDialogAsync();
        }
    }
}
