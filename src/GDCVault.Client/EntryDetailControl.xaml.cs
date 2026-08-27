using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using GDCVault.Core.Models;
using GDCVault.Core.Services;
using Microsoft.Win32;
using Clipboard = System.Windows.Clipboard;

namespace GDCVault.Client;

/// Stare UI locala per cont suplimentar - parola e citita/scrisa direct
/// din DPAPI (vezi VaultDpapiStore.*CredentialSecret), nu tine de
/// LoginCredential (care are doar HasPassword: bool, fara secret).
public sealed class AdditionalLoginRow
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Label { get; set; } = "";
    public string? LoginUrl { get; set; }
    public string? Username { get; set; }
    public string Password { get; set; } = "";
}

/// Oglinda EntryDetailView.swift (Mac) - fisa unificata de produs,
/// embedata direct in panoul de detaliu (nu o fereastra modala separata).
public partial class EntryDetailControl : UserControl
{
    private readonly VaultMetadataStore _store;
    private readonly Guid _entryId;
    private readonly bool _isNew;
    private List<AttachmentRef> _attachments;
    private readonly ObservableCollection<PurchasedAsset> _assets;
    private readonly ObservableCollection<AdditionalLoginRow> _additionalLogins;
    private readonly HashSet<Guid> _originalCredentialIds;

    // PITFALL FIXED 2026-08-24 (bug critic de UX): PasswordBox NU poate fi
    // legat prin binding (WPF il interzice intentionat, motiv de
    // securitate), deci valoarea reala trebuie tinuta separat in cod ca sa
    // supravietuiasca toggle-ului ascuns/vizibil (PasswordBox <-> TextBox).
    // Fara asta, userul NU putea revedea parola/seria deja salvata.
    private string _passwordValue = "";
    private bool _passwordRevealed;
    private string _serialValue = "";
    private bool _serialRevealed;

    public event Action<VaultEntry>? Saved;
    public event Action? Deleted;
    public event Action? CanceledNew;

    public EntryDetailControl(VaultMetadataStore store, VaultEntry initialEntry, bool isNew)
    {
        InitializeComponent();
        _store = store;
        _isNew = isNew;
        _entryId = initialEntry.Id;
        _attachments = new List<AttachmentRef>(initialEntry.Attachments);
        _assets = new ObservableCollection<PurchasedAsset>(initialEntry.PurchasedAssets);
        AssetsItemsControl.ItemsSource = _assets;

        _additionalLogins = new ObservableCollection<AdditionalLoginRow>(initialEntry.AdditionalLogins.Select(cred => new AdditionalLoginRow
        {
            Id = cred.Id,
            Label = cred.Label,
            LoginUrl = cred.LoginUrl,
            Username = cred.Username,
            Password = cred.HasPassword ? (VaultDpapiStore.ReadCredentialSecret(initialEntry.Id, cred.Id) ?? "") : ""
        }));
        _originalCredentialIds = initialEntry.AdditionalLogins.Select(c => c.Id).ToHashSet();
        AdditionalLoginsItemsControl.ItemsSource = _additionalLogins;

        NameBox.Text = initialEntry.Name;
        LoginUrlBox.Text = initialEntry.LoginUrl ?? "";
        UsernameBox.Text = initialEntry.Username ?? "";

        _passwordValue = initialEntry.HasPassword
            ? VaultDpapiStore.Read(_entryId, VaultDpapiStore.SecretSlot.Password) ?? "" : "";
        PasswordBox.Password = _passwordValue;
        _serialValue = initialEntry.HasSerial
            ? VaultDpapiStore.Read(_entryId, VaultDpapiStore.SecretSlot.Serial) ?? "" : "";
        SerialBox.Password = _serialValue;

        LicenseTypeCombo.ItemsSource = Enum.GetValues<LicenseType>()
            .Select(t => new { Value = t, Display = t.DisplayName() })
            .ToList();
        LicenseTypeCombo.DisplayMemberPath = "Display";
        LicenseTypeCombo.SelectedValuePath = "Value";
        LicenseTypeCombo.SelectedValue = initialEntry.LicenseType;

        HasExpiryCheck.IsChecked = initialEntry.ExpiresAt is not null;
        ExpiryPicker.SelectedDate = initialEntry.ExpiresAt?.DateTime ?? DateTime.Today;
        DownloadUrlBox.Text = initialEntry.DownloadUrl ?? "";
        UpdateUrlBox.Text = initialEntry.UpdateUrl ?? "";
        NotesBox.Text = initialEntry.Notes ?? "";

        DeleteButton.Visibility = isNew ? Visibility.Collapsed : Visibility.Visible;
        CancelButton.Visibility = isNew ? Visibility.Visible : Visibility.Collapsed;
        UpdateExpiryEnabled();
        RefreshAttachmentsList();
    }

    private void OnPasswordBoxChanged(object sender, RoutedEventArgs e) => _passwordValue = PasswordBox.Password;
    private void OnPasswordRevealChanged(object sender, TextChangedEventArgs e) => _passwordValue = PasswordRevealBox.Text;

    private void OnTogglePasswordReveal(object sender, RoutedEventArgs e)
    {
        _passwordRevealed = !_passwordRevealed;
        if (_passwordRevealed)
        {
            PasswordRevealBox.Text = _passwordValue;
            PasswordRevealBox.Visibility = Visibility.Visible;
            PasswordBox.Visibility = Visibility.Collapsed;
        }
        else
        {
            PasswordBox.Password = _passwordValue;
            PasswordBox.Visibility = Visibility.Visible;
            PasswordRevealBox.Visibility = Visibility.Collapsed;
        }
    }

    private void OnCopyPasswordClicked(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(_passwordValue)) Clipboard.SetText(_passwordValue);
    }

    private void OnSerialBoxChanged(object sender, RoutedEventArgs e) => _serialValue = SerialBox.Password;
    private void OnSerialRevealChanged(object sender, TextChangedEventArgs e) => _serialValue = SerialRevealBox.Text;

    private void OnToggleSerialReveal(object sender, RoutedEventArgs e)
    {
        _serialRevealed = !_serialRevealed;
        if (_serialRevealed)
        {
            SerialRevealBox.Text = _serialValue;
            SerialRevealBox.Visibility = Visibility.Visible;
            SerialBox.Visibility = Visibility.Collapsed;
        }
        else
        {
            SerialBox.Password = _serialValue;
            SerialBox.Visibility = Visibility.Visible;
            SerialRevealBox.Visibility = Visibility.Collapsed;
        }
    }

    private void OnCopySerialClicked(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(_serialValue)) Clipboard.SetText(_serialValue);
    }

    private void OnExpiryToggled(object sender, RoutedEventArgs e) => UpdateExpiryEnabled();
    private void UpdateExpiryEnabled() => ExpiryPicker.IsEnabled = HasExpiryCheck.IsChecked == true;

    private void RefreshAttachmentsList()
    {
        AttachmentsList.ItemsSource = null;
        AttachmentsList.ItemsSource = _attachments.Select(a => a.OriginalFileName).ToList();
    }

    private void OnAddAttachmentClicked(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Multiselect = true,
            Filter = "Documente și imagini (*.pdf;*.png;*.jpg;*.jpeg)|*.pdf;*.png;*.jpg;*.jpeg|Toate fișierele (*.*)|*.*",
            Title = "Alege contracte, facturi sau screenshot-uri de atașat"
        };
        if (dialog.ShowDialog() != true) return;

        foreach (var path in dialog.FileNames)
        {
            _attachments.Add(AttachmentStore.Add(path, _entryId));
        }
        RefreshAttachmentsList();
    }

    private void OnRemoveAttachmentClicked(object sender, RoutedEventArgs e)
    {
        var index = AttachmentsList.SelectedIndex;
        if (index < 0 || index >= _attachments.Count) return;

        var attachment = _attachments[index];
        AttachmentStore.Remove(attachment, _entryId);
        _attachments.RemoveAt(index);
        RefreshAttachmentsList();
    }

    private void OnAttachmentDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        var index = AttachmentsList.SelectedIndex;
        if (index < 0 || index >= _attachments.Count) return;

        var path = AttachmentStore.FilePath(_attachments[index], _entryId);
        if (File.Exists(path))
        {
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }
    }

    private void OnAddAdditionalLoginClicked(object sender, RoutedEventArgs e) => _additionalLogins.Add(new AdditionalLoginRow());

    private void OnRemoveAdditionalLoginClicked(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is AdditionalLoginRow row) _additionalLogins.Remove(row);
    }

    private void OnAddAssetClicked(object sender, RoutedEventArgs e) => _assets.Add(new PurchasedAsset());

    private void OnRemoveAssetClicked(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is PurchasedAsset asset) _assets.Remove(asset);
    }

    /// `Microsoft.Win32.OpenFolderDialog` — disponibil nativ din .NET 8
    /// pentru WPF, oglinda NSOpenPanel(canChooseDirectories) de pe Mac.
    private void OnPickAssetFolderClicked(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is not PurchasedAsset asset) return;
        var dialog = new OpenFolderDialog { Title = "Alege folderul local unde ai salvat acest asset/pachet." };
        if (dialog.ShowDialog() != true) return;
        asset.FolderPath = dialog.FolderName;

        // ItemsControl-ul nu are ObservableObject pe PurchasedAsset — fortam
        // refresh vizual simplu, reasignand sursa (colectia ramane aceeasi).
        AssetsItemsControl.Items.Refresh();
    }

    private void OnOpenAssetFolderClicked(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is not PurchasedAsset asset) return;
        if (string.IsNullOrEmpty(asset.FolderPath) || !Directory.Exists(asset.FolderPath)) return;
        Process.Start(new ProcessStartInfo(asset.FolderPath) { UseShellExecute = true });
    }

    private void OnCancelClicked(object sender, RoutedEventArgs e) => CanceledNew?.Invoke();

    private void OnDeleteClicked(object sender, RoutedEventArgs e)
    {
        var existing = _store.Entries.FirstOrDefault(x => x.Id == _entryId);
        if (existing is not null) _store.Delete(existing);
        Deleted?.Invoke();
    }

    private void OnSaveClicked(object sender, RoutedEventArgs e)
    {
        var name = NameBox.Text.Trim();
        if (string.IsNullOrEmpty(name)) return;

        var entry = new VaultEntry
        {
            Id = _entryId,
            Name = name,
            LoginUrl = string.IsNullOrEmpty(LoginUrlBox.Text) ? null : LoginUrlBox.Text,
            Username = string.IsNullOrEmpty(UsernameBox.Text) ? null : UsernameBox.Text,
            LicenseType = (LicenseType)(LicenseTypeCombo.SelectedValue ?? LicenseType.None),
            ExpiresAt = HasExpiryCheck.IsChecked == true ? ExpiryPicker.SelectedDate : null,
            DownloadUrl = string.IsNullOrEmpty(DownloadUrlBox.Text) ? null : DownloadUrlBox.Text,
            UpdateUrl = string.IsNullOrEmpty(UpdateUrlBox.Text) ? null : UpdateUrlBox.Text,
            Notes = string.IsNullOrEmpty(NotesBox.Text) ? null : NotesBox.Text,
            Attachments = _attachments,
            PurchasedAssets = _assets.ToList(),
            AdditionalLogins = _additionalLogins.Select(row => new LoginCredential
            {
                Id = row.Id,
                Label = row.Label,
                LoginUrl = string.IsNullOrEmpty(row.LoginUrl) ? null : row.LoginUrl,
                Username = string.IsNullOrEmpty(row.Username) ? null : row.Username,
                HasPassword = !string.IsNullOrEmpty(row.Password)
            }).ToList()
        };

        // Camp gol la Salveaza = "fara secret" - semantica directa (ce vezi
        // e ce se salveaza), nu "gol = nu schimba" (bug de UX fixat
        // 2026-08-24: campul era populat mereu la deschidere, deci vidarea
        // lui e o alegere explicita de a sterge secretul).
        if (string.IsNullOrEmpty(_passwordValue))
        {
            VaultDpapiStore.Delete(_entryId, VaultDpapiStore.SecretSlot.Password);
            entry.HasPassword = false;
        }
        else
        {
            VaultDpapiStore.Save(_passwordValue, _entryId, VaultDpapiStore.SecretSlot.Password);
            entry.HasPassword = true;
        }

        if (string.IsNullOrEmpty(_serialValue))
        {
            VaultDpapiStore.Delete(_entryId, VaultDpapiStore.SecretSlot.Serial);
            entry.HasSerial = false;
        }
        else
        {
            VaultDpapiStore.Save(_serialValue, _entryId, VaultDpapiStore.SecretSlot.Serial);
            entry.HasSerial = true;
        }

        // Conturi suplimentare: scrie/sterge parola fiecarui rand in
        // fisierul DPAPI propriu, apoi curata secretele randurilor
        // ELIMINATE de user in aceasta sesiune de editare.
        foreach (var row in _additionalLogins)
        {
            if (string.IsNullOrEmpty(row.Password))
                VaultDpapiStore.DeleteCredentialSecret(_entryId, row.Id);
            else
                VaultDpapiStore.SaveCredentialSecret(row.Password, _entryId, row.Id);
        }
        var currentCredentialIds = _additionalLogins.Select(r => r.Id).ToHashSet();
        foreach (var removedId in _originalCredentialIds.Except(currentCredentialIds))
        {
            VaultDpapiStore.DeleteCredentialSecret(_entryId, removedId);
        }

        _store.Upsert(entry);
        Saved?.Invoke(entry);
    }
}
