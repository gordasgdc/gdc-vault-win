using System.Windows;
using GDCVault.Core.Models;
using GDCVault.Core.Services;
using Microsoft.Win32;

namespace GDCVault.Client;

public partial class MainWindow
{
    private readonly VaultMetadataStore _store = new();
    private readonly LicenseManager _license = LicenseManager.Shared;
    private VaultEntry? _draftEntry; // non-null cat timp se completeaza o intrare noua, inca nesalvata

    public MainWindow()
    {
        InitializeComponent();
        _license.Changed += RefreshTrialBanner;
        RefreshTrialBanner();
        Reload();
        ShowEmptyState();
        VersionText.Text = $"v{UpdateChecker.CurrentVersion}";
        Loaded += async (_, _) => await MaybeShowUpdatePopupAsync(respectDismissal: true);
    }

    /// Verificare automata la lansare + pop-up real (nu doar text discret
    /// in footer) daca exista o versiune noua - Directiva Permanenta
    /// Suprema. Foloseste Wpf.Ui.Controls.MessageBox (nu System.Windows.
    /// MessageBox - acela nu poate arata text custom pe butoane).
    /// `respectDismissal`: true la lansarea automata (nu reapare pt. o
    /// versiune deja inchisa), false la click manual pe "Caută
    /// actualizări" (mereu arata rezultatul real, indiferent de dismissal).
    private async Task MaybeShowUpdatePopupAsync(bool respectDismissal, bool announceIfUpToDate = false)
    {
        await UpdateChecker.Shared.CheckAsync();
        var version = UpdateChecker.Shared.AvailableVersion;
        if (version is null || (respectDismissal && UpdateChecker.Shared.WasDismissed(version)))
        {
            if (announceIfUpToDate)
            {
                await new Wpf.Ui.Controls.MessageBox { Title = "Ești la zi", Content = $"Rulezi deja ultima versiune (v{UpdateChecker.CurrentVersion})." }.ShowDialogAsync();
            }
            return;
        }

        var box = new Wpf.Ui.Controls.MessageBox
        {
            Title = "Este disponibilă o versiune nouă",
            Content = $"GDC Vault {version} este disponibil (tu ai {UpdateChecker.CurrentVersion}). " +
                      "Te rugăm să descarci ultimul installer și să îl instalezi peste versiunea actuală.",
            PrimaryButtonText = "Descarcă",
            CloseButtonText = "Mai târziu",
        };
        var result = await box.ShowDialogAsync();
        if (result == Wpf.Ui.Controls.MessageBoxResult.Primary)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(UpdateChecker.ReleasesPageUrl.ToString()) { UseShellExecute = true });
        }
        UpdateChecker.Shared.Dismiss();
    }

    private async void OnCheckForUpdatesClicked(object sender, RoutedEventArgs e) =>
        await MaybeShowUpdatePopupAsync(respectDismissal: false, announceIfUpToDate: true);

    /// Vezi LicenseManager.cs pentru rationament: banner-ul nu blocheaza
    /// nimic singur — doar butonul "+ Adauga aplicatie" verifica IsUnlocked.
    private void RefreshTrialBanner()
    {
        if (_license.IsLicensed)
        {
            TrialBanner.Visibility = Visibility.Collapsed;
            return;
        }
        TrialBanner.Visibility = Visibility.Visible;
        TrialBannerText.Text = _license.IsTrialActive
            ? $"Probă gratuită — {_license.TrialDaysRemaining} zile rămase"
            : "Proba a expirat — poți vizualiza și exporta datele existente";
    }

    private void OnActivateClicked(object sender, RoutedEventArgs e)
    {
        new ActivationWindow(_license) { Owner = this }.ShowDialog();
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
        if (!_license.IsUnlocked)
        {
            new ActivationWindow(_license) { Owner = this }.ShowDialog();
            return;
        }
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
