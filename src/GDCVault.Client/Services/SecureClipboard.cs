using System.Windows;
using System.Windows.Threading;

namespace GDCVault.Client.Services;

/// Copiere in clipboard cu golire automata.
///
/// DE CE cu verificare inainte de golire: daca intre timp ai copiat altceva,
/// o golire oarba ti-ar arunca ce ai copiat TU. Pe Windows nu exista un
/// contor de schimbari ca pe Mac, deci comparam continutul.
public static class SecureClipboard
{
    public static void Copy(string? value, int clearAfterSeconds = 45)
    {
        if (string.IsNullOrEmpty(value)) return;

        // Clipboard-ul Windows poate fi blocat momentan de alt proces —
        // o exceptie aici nu are voie sa dea jos aplicatia.
        try { Clipboard.SetText(value); } catch { return; }

        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(clearAfterSeconds) };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            try
            {
                if (Clipboard.ContainsText() && Clipboard.GetText() == value) Clipboard.Clear();
            }
            catch { /* la fel: golirea e best-effort, nu un motiv de crash */ }
        };
        timer.Start();
    }
}
