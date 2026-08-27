using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Windows;
using GDCVault.Core.Services;

namespace GDCVault.Client;

/// Descarca si lanseaza automat installer-ul de update, fara sa mai treaca
/// prin browser/pagina de GitHub — port 1:1 al SelfUpdater.cs din
/// GDCPluginManagerWin (vezi CLAUDE.md Partea 1).
///
/// BUG FIX 2026-08-27 (raportat de Cristi, cu screenshot: popup-ul de
/// update tot deschidea github.com in browser). Fluxul aici:
///   1. Descarca `UpdateChecker.DirectDownloadUrl` (GDCVaultSetup.exe,
///      nume stabil) cu HttpClient direct pe disc, redenumit cu versiunea
///      (Regula 17).
///   2. Lanseaza installer-ul (Process.Start, UseShellExecute=true) -
///      fereastra Inno Setup apare, dar NICIODATA browserul/GitHub.
///   3. Inchide aplicatia curenta - installer.iss are deja
///      `[Run] ... Flags: nowait postinstall skipifsilent`, care
///      relanseaza aplicatia dupa instalare; fara AppMutex/
///      CloseApplications configurat, Setup.exe nu poate suprascrie
///      singur exe-ul cat timp ruleaza, deci trebuie sa ne inchidem noi.
///
/// WARNING: pasul de instalare efectiv (wizard-ul Inno, click-urile
/// userului) NU poate fi verificat automat de Claude.
public static class SelfUpdater
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromMinutes(5) };

    public static async Task DownloadAndInstallAsync(string version)
    {
        var progress = new UpdateProgressWindow(version);
        progress.Show();

        try
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "gdcvault-update-" + Guid.NewGuid());
            Directory.CreateDirectory(tempDir);

            progress.SetStatus("Se descarcă actualizarea…");
            var exePath = Path.Combine(tempDir, $"GDCVaultSetup-{version}.exe");
            await DownloadAsync(UpdateChecker.DirectDownloadUrl.ToString(), exePath);

            progress.SetStatus("Se lansează instalatorul…");
            Process.Start(new ProcessStartInfo(exePath) { UseShellExecute = true });

            progress.Close();
            Application.Current.Shutdown();
        }
        catch (Exception ex)
        {
            progress.Close();
            PresentFailure(ex.Message);
        }
    }

    private static async Task DownloadAsync(string url, string destination)
    {
        using var response = await Http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Descărcarea a eșuat: HTTP {(int)response.StatusCode}");
        }
        await using var httpStream = await response.Content.ReadAsStreamAsync();
        await using var fileStream = File.Create(destination);
        await httpStream.CopyToAsync(fileStream);
    }

    private static void PresentFailure(string message)
    {
        var box = new Wpf.Ui.Controls.MessageBox
        {
            Title = "Actualizarea a eșuat",
            Content = $"{message}\n\nPoți descărca manual ultima versiune de pe pagina de GitHub.",
            PrimaryButtonText = "Deschide pagina",
            CloseButtonText = "OK",
        };
        _ = ShowFailureAsync(box);
    }

    private static async Task ShowFailureAsync(Wpf.Ui.Controls.MessageBox box)
    {
        var result = await box.ShowDialogAsync();
        if (result == Wpf.Ui.Controls.MessageBoxResult.Primary)
        {
            Process.Start(new ProcessStartInfo(UpdateChecker.ReleasesPageUrl.ToString()) { UseShellExecute = true });
        }
    }
}
