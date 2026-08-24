using System.Windows;

namespace GDCVault.Client;

/// Echivalentul PasswordPromptWindow.swift (Mac) — o fereastra mica,
/// modala, cu un singur PasswordBox. Folosita pentru parola Master de
/// export/import (vezi VaultExportImport).
public partial class PasswordPromptWindow
{
    public string? EnteredPassword { get; private set; }

    public PasswordPromptWindow(string title, string message)
    {
        InitializeComponent();
        Title = title;
        MessageText.Text = message;
    }

    private void OnOkClicked(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(PasswordField.Password)) return;
        EnteredPassword = PasswordField.Password;
        DialogResult = true;
        Close();
    }

    private void OnCancelClicked(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    /// Helper static — deschide fereastra modal si intoarce parola, sau
    /// null daca userul a anulat.
    public static string? Ask(Window owner, string title, string message)
    {
        var window = new PasswordPromptWindow(title, message) { Owner = owner };
        return window.ShowDialog() == true ? window.EnteredPassword : null;
    }
}
