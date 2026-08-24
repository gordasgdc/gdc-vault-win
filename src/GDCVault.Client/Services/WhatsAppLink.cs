namespace GDCVault.Client.Services;

/// Oglinda WhatsAppLink.swift (Mac) — acelasi numar de contact,
/// reconstruit din bucati (nu ca literal simplu), ca sa nu apara ca sir
/// contiguu intr-un repo public usor de scanat de crawlere de spam.
public static class WhatsAppLink
{
    private static readonly string[] Parts = ["34", "643", "109", "970"];
    private static string Number => string.Concat(Parts);

    public static string Url(string? text = null)
    {
        var baseUrl = $"https://wa.me/{Number}";
        return text is null ? baseUrl : $"{baseUrl}?text={Uri.EscapeDataString(text)}";
    }
}
