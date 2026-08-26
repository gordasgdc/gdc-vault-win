using System.Windows;
using GDCVault.Core.Services;

namespace GDCVault.Client;

/// Editorul de Profil Utilizator (Nume/Email opționale) din sidebar —
/// port 1:1 al popover-ului din ProfileSidebarBlock.swift (Mac).
public partial class ProfileEditWindow
{
    public ProfileEditWindow()
    {
        InitializeComponent();
        NameBox.Text = UserProfileStore.Shared.Name;
        EmailBox.Text = UserProfileStore.Shared.Email;
        MachineIdBox.Text = UserProfileStore.Shared.MachineId;
    }

    private void OnSaveClicked(object sender, RoutedEventArgs e)
    {
        UserProfileStore.Shared.Save(NameBox.Text, EmailBox.Text, sendTelemetry: true);
        DialogResult = true;
        Close();
    }

    private void OnCancelClicked(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
