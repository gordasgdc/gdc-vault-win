using System.Windows;
using System.Windows.Media;
using Wpf.Ui.Appearance;

namespace GDCVault.Client;

public partial class App : Application
{
    /// Tema "Shift" (2026-08-26, vezi CLAUDE.md Partea 1 Regula 7/12) —
    /// aplicată peste tema Dark implicită a Wpf.Ui, care oricum ar folosi
    /// accentul Windows implicit (albastru) fără asta. Aplicarea explicită
    /// a accentului amber/cupru face TOATE controalele Wpf.Ui (butoane,
    /// checkbox-uri, focus ring) să folosească automat aceeași paletă ca
    /// Mac (Theme.swift) și gordas.dev, fără să reimplementăm fiecare
    /// stil manual.
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ApplicationAccentColorManager.Apply(Color.FromRgb(0xE8, 0x96, 0x3C));
    }
}
