using System.Windows;
using GDCVault.Client.Services;

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
        _loaded = true;
    }

    private void OnThemeChanged(object sender, RoutedEventArgs e)
    {
        if (!_loaded) return;
        if (ThemeLightRadio.IsChecked == true) ThemeManager.Set(AppTheme.Light);
        else if (ThemeDarkRadio.IsChecked == true) ThemeManager.Set(AppTheme.Dark);
        else ThemeManager.Set(AppTheme.System);
    }

    private void OnOpenHelpClicked(object sender, RoutedEventArgs e) => HelpGuide.Open();

    private void OnCloseClicked(object sender, RoutedEventArgs e) => Close();
}
