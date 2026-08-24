using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using GDCVault.Core.Models;
using GDCVault.Core.Services;
using Microsoft.Win32;

namespace GDCVault.Client;

/// Oglinda EntryDetailView.swift (Mac) - fisa unificata de produs,
/// embedata direct in panoul de detaliu (nu o fereastra modala separata).
public partial class EntryDetailControl : UserControl
{
    private readonly VaultMetadataStore _store;
    private readonly Guid _entryId;
    private readonly bool _isNew;
    private List<AttachmentRef> _attachments;

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

        NameBox.Text = initialEntry.Name;
        LoginUrlBox.Text = initialEntry.LoginUrl ?? "";
        UsernameBox.Text = initialEntry.Username ?? "";
        PasswordLabel.Text = isNew ? "Parolă (opțional)" : "Parolă nouă (gol = nu o schimba)";
        SerialLabel.Text = isNew ? "Cheie de serie (opțional)" : "Cheie de serie nouă (gol = nu o schimba)";

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

        var existing = _store.Entries.FirstOrDefault(x => x.Id == _entryId);

        var entry = new VaultEntry
        {
            Id = _entryId,
            Name = name,
            LoginUrl = string.IsNullOrEmpty(LoginUrlBox.Text) ? null : LoginUrlBox.Text,
            Username = string.IsNullOrEmpty(UsernameBox.Text) ? null : UsernameBox.Text,
            HasPassword = existing?.HasPassword ?? false,
            LicenseType = (LicenseType)(LicenseTypeCombo.SelectedValue ?? LicenseType.None),
            ExpiresAt = HasExpiryCheck.IsChecked == true ? ExpiryPicker.SelectedDate : null,
            HasSerial = existing?.HasSerial ?? false,
            DownloadUrl = string.IsNullOrEmpty(DownloadUrlBox.Text) ? null : DownloadUrlBox.Text,
            UpdateUrl = string.IsNullOrEmpty(UpdateUrlBox.Text) ? null : UpdateUrlBox.Text,
            Notes = string.IsNullOrEmpty(NotesBox.Text) ? null : NotesBox.Text,
            Attachments = _attachments
        };

        if (!string.IsNullOrEmpty(PasswordBox.Password))
        {
            VaultDpapiStore.Save(PasswordBox.Password, _entryId, VaultDpapiStore.SecretSlot.Password);
            entry.HasPassword = true;
        }
        if (!string.IsNullOrEmpty(SerialBox.Password))
        {
            VaultDpapiStore.Save(SerialBox.Password, _entryId, VaultDpapiStore.SecretSlot.Serial);
            entry.HasSerial = true;
        }

        _store.Upsert(entry);
        Saved?.Invoke(entry);
    }
}
