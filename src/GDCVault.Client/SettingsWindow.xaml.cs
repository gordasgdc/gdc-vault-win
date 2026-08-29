using System.Windows;
using System.Windows.Controls;
using GDCVault.Client.Services;
using GDCVault.Core.Services;

namespace GDCVault.Client;

public partial class SettingsWindow
{
    private bool _loaded;

    public SettingsWindow()
    {
        InitializeComponent();
        switch (ThemeManager.Current)
        {
            case AppTheme.Light: ThemeLightRadio.IsChecked = true; break;
            case AppTheme.Dark: ThemeDarkRadio.IsChecked = true; break;
            default: ThemeSystemRadio.IsChecked = true; break;
        }

        var currentScale = TextScaleStore.Load();
        foreach (ComboBoxItem item in TextScaleCombo.Items)
        {
            if (Enum.TryParse<TextScalePreference>((string)item.Tag, out var value) && value == currentScale)
            {
                TextScaleCombo.SelectedItem = item;
                break;
            }
        }

        _loaded = true;
    }

    private void OnThemeChanged(object sender, RoutedEventArgs e)
    {
        if (!_loaded) return;
        if (ThemeLightRadio.IsChecked == true) ThemeManager.Set(AppTheme.Light);
        else if (ThemeDarkRadio.IsChecked == true) ThemeManager.Set(AppTheme.Dark);
        else ThemeManager.Set(AppTheme.System);
    }

    private void OnTextScaleChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_loaded) return;
        if (TextScaleCombo.SelectedItem is not ComboBoxItem item) return;
        if (!Enum.TryParse<TextScalePreference>((string)item.Tag, out var preference)) return;

        TextScaleStore.Save(preference);
        if (Owner is MainWindow mainWindow)
        {
            mainWindow.ApplyTextScale(preference);
        }
    }

    private void OnOpenHelpClicked(object sender, RoutedEventArgs e) => HelpGuide.Open();

    private void OnCloseClicked(object sender, RoutedEventArgs e) => Close();
}
