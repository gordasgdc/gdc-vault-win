using System;
using System.IO;
using Wpf.Ui.Appearance;

namespace GDCVault.Client.Services;

/// Selector explicit Dark/Light/System, independent de tema Windows
/// (2026-08-27, cerință Cristi) — oglinda ThemeManager.swift (Mac).
/// Foloseste `Wpf.Ui.Appearance.ApplicationThemeManager` (nativ pachetului
/// deja folosit de toate controalele — vezi App.xaml.cs), nu manipulare
/// manuala de resurse. "Sistem" citeste tema Windows curenta o singura
/// data la selectare (GetSystemTheme()) - nu urmareste live schimbarea
/// temei Windows in timp ce aplicatia ruleaza, spre deosebire de Mac unde
/// `NSApp.appearance = nil` urmeaza dinamic sistemul.
public enum AppTheme { System, Light, Dark }

public static class ThemeManager
{
    private static readonly string SettingsFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "GDC Vault", "theme.txt");

    public static AppTheme Current { get; private set; } = Load();

    public static void Apply()
    {
        var resolved = Current == AppTheme.System
            ? (ApplicationThemeManager.GetSystemTheme() == SystemTheme.Light ? ApplicationTheme.Light : ApplicationTheme.Dark)
            : (Current == AppTheme.Light ? ApplicationTheme.Light : ApplicationTheme.Dark);
        ApplicationThemeManager.Apply(resolved);
    }

    public static void Set(AppTheme theme)
    {
        Current = theme;
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsFilePath)!);
        File.WriteAllText(SettingsFilePath, theme.ToString());
        Apply();
    }

    private static AppTheme Load()
    {
        if (File.Exists(SettingsFilePath) && Enum.TryParse<AppTheme>(File.ReadAllText(SettingsFilePath).Trim(), out var saved))
        {
            return saved;
        }
        return AppTheme.System;
    }
}
