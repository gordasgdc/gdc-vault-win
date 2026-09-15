using System.Diagnostics;
using GDCVault.Core.Models;

namespace GDCVault.Client.Services;

public static class UrlLauncher
{
    public static bool CanLaunch(string? text) => VaultEntry.IsLaunchableUrl(text);

    /// `UseShellExecute = true` e obligatoriu pe .NET Core: fara el,
    /// Process.Start cu un URL arunca Win32Exception in loc sa deschida ceva.
    public static void Launch(string? text)
    {
        if (!CanLaunch(text)) return;
        Process.Start(new ProcessStartInfo(text!.Trim()) { UseShellExecute = true });
    }
}
