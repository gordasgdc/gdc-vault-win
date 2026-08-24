using System.Diagnostics;
using System.IO;
using System.Windows;
using GDCVault.Core.Models;
using GDCVault.Core.Services;
using Microsoft.Win32;

namespace GDCVault.Client;

/// Formular unic pentru toate cele 3 tipuri, la fel ca EntryEditorView.swift
/// (Mac) - vezi comentariile de acolo pentru rationament. Code-behind simplu
/// (nu MVVM complet) tocmai pentru ca e "prima versiune de baza" - de
/// extins cu ViewModel-uri dedicate cand adaugam validari mai complexe.
public partial class EntryEditorWindow
{
    private readonly VaultMetadataStore _store;
    private readonly Guid _entryId;
    private readonly bool _isNew;
    private List<AttachmentRef> _attachments;

    public EntryEditorWindow(VaultMetadataStore store, VaultEntry? existing)
    {
        InitializeComponent();
        _store = store;
        _isNew = existing is null;
        var entry = existing ?? new VaultEntry { Kind = VaultEntryKind.PerpetualLicense };
        _entryId = entry.Id;

        KindCombo.ItemsSource = Enum.GetValues<VaultEntryKind>()
            .Select(k => new { Value = k, Display = k.DisplayName() })
            .ToList();
        KindCombo.DisplayMemberPath = "Display";
        KindCombo.SelectedValuePath = "Value";
        KindCombo.SelectedValue = entry.Kind;

        NameBox.Text = entry.Name;
        HasExpiryCheck.IsChecked = entry.ExpiresAt is not null;
        ExpiryPicker.SelectedDate = entry.ExpiresAt?.DateTime ?? DateTime.Today;
        LoginUrlBox.Text = entry.LoginUrl ?? "";
        UsernameBox.Text = entry.Username ?? "";
        DownloadUrlBox.Text = entry.DownloadUrl ?? "";
        UpdateUrlBox.Text = entry.UpdateUrl ?? "";
        NotesBox.Text = entry.Notes ?? "";
        _attachments = new List<AttachmentRef>(entry.Attachments);
        RefreshAttachmentsList();

        DeleteButton.Visibility = _isNew ? Visibility.Collapsed : Visibility.Visible;
        UpdateFieldVisibility();
        UpdateExpiryEnabled();
    }

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

    private void OnKindChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) => UpdateFieldVisibility();
    private void OnExpiryToggled(object sender, RoutedEventArgs e) => UpdateExpiryEnabled();

    private void UpdateFieldVisibility()
    {
        var isCredential = (VaultEntryKind?)KindCombo.SelectedValue == VaultEntryKind.Credential;
        CredentialFields.Visibility = isCredential ? Visibility.Visible : Visibility.Collapsed;
        LicenseFields.Visibility = isCredential ? Visibility.Collapsed : Visibility.Visible;
    }

    private void UpdateExpiryEnabled() => ExpiryPicker.IsEnabled = HasExpiryCheck.IsChecked == true;

    private void OnCancelClicked(object sender, RoutedEventArgs e) => Close();

    private void OnDeleteClicked(object sender, RoutedEventArgs e)
    {
        var existing = _store.Entries.FirstOrDefault(x => x.Id == _entryId);
        if (existing is not null) _store.Delete(existing);
        Close();
    }

    private void OnSaveClicked(object sender, RoutedEventArgs e)
    {
        var name = NameBox.Text.Trim();
        if (string.IsNullOrEmpty(name)) return;

        var kind = (VaultEntryKind)(KindCombo.SelectedValue ?? VaultEntryKind.PerpetualLicense);
        var entry = new VaultEntry
        {
            Id = _entryId,
            Kind = kind,
            Name = name,
            ExpiresAt = HasExpiryCheck.IsChecked == true ? ExpiryPicker.SelectedDate : null,
            LoginUrl = string.IsNullOrEmpty(LoginUrlBox.Text) ? null : LoginUrlBox.Text,
            Username = string.IsNullOrEmpty(UsernameBox.Text) ? null : UsernameBox.Text,
            DownloadUrl = string.IsNullOrEmpty(DownloadUrlBox.Text) ? null : DownloadUrlBox.Text,
            UpdateUrl = string.IsNullOrEmpty(UpdateUrlBox.Text) ? null : UpdateUrlBox.Text,
            Notes = string.IsNullOrEmpty(NotesBox.Text) ? null : NotesBox.Text,
            Attachments = _attachments
        };

        var secretToSave = kind == VaultEntryKind.Credential ? SecretBox.Password : LicenseSecretBox.Password;
        if (!string.IsNullOrEmpty(secretToSave))
        {
            VaultDpapiStore.Save(secretToSave, _entryId);
            entry.HasSecret = true;
        }
        else
        {
            entry.HasSecret = _store.Entries.FirstOrDefault(x => x.Id == _entryId)?.HasSecret ?? false;
        }

        _store.Upsert(entry);
        Close();
    }
}
