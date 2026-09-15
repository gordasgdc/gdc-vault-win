using System.Globalization;
using System.Text;
using GDCVault.Core.Models;

namespace GDCVault.Core.Services;

/// Export in calendar (.ics) pentru expirari.
///
/// Fisier .ics, nu API de calendar: il deschizi tu, vezi ce contine si alegi
/// in ce calendar intra — si se sincronizeaza pe telefon, unde il vezi si cu
/// calculatorul inchis. Identic cu varianta de pe Mac.
public static class RenewalReminders
{
    public static string? IcsText(VaultEntry entry)
    {
        if (entry.ExpiresAt is not { } expiresAt) return null;

        var lines = new List<string>
        {
            "BEGIN:VCALENDAR", "VERSION:2.0", "PRODID:-//GDC//Vault//RO",
            "BEGIN:VEVENT",
            $"UID:{entry.Id}@gdcvault",
            $"DTSTAMP:{DateTime.UtcNow.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture)}",
            $"DTSTART;VALUE=DATE:{expiresAt.ToString("yyyyMMdd", CultureInfo.InvariantCulture)}",
            $"SUMMARY:Reînnoire {Escape(entry.Name)}",
        };

        if (entry.PriceDisplay is { } price) lines.Add($"DESCRIPTION:Cost: {Escape(price)}");

        foreach (var days in entry.ReminderDaysBefore.OrderByDescending(d => d))
        {
            lines.AddRange(new[]
            {
                "BEGIN:VALARM", "ACTION:DISPLAY",
                $"DESCRIPTION:{Escape(entry.Name)} expiră în {days} zile",
                $"TRIGGER:-P{days}D", "END:VALARM",
            });
        }

        lines.Add("END:VEVENT");
        lines.Add("END:VCALENDAR");
        // CRLF, nu \n: cerut de RFC 5545, iar unele calendare refuza altfel fisierul.
        return string.Join("\r\n", lines) + "\r\n";
    }

    /// `,` `;` `\` si newline au inteles sintactic in .ics — netratate, rup fisierul.
    private static string Escape(string text) => text
        .Replace("\\", "\\\\")
        .Replace(";", "\\;")
        .Replace(",", "\\,")
        .Replace("\n", "\\n");

    public static void WriteIcs(VaultEntry entry, string path)
    {
        var text = IcsText(entry);
        if (text is null) return;
        File.WriteAllText(path, text, new UTF8Encoding(false));
    }
}
