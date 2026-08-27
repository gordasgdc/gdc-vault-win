using System.Globalization;
using System.Text;
using GDCVault.Core.Models;

namespace GDCVault.Core.Services;

/// Oglinda FuzzySearch.swift (Mac) — potrivire tolerantă la greșeli de
/// tipar: substring direct, apoi subsecvență de caractere în ordine
/// (gen "epic sound" -> "Epidemic Sound") dacă substring-ul direct nu
/// există. Insensibilă la majuscule/diacritice și la spații din query.
public static class FuzzySearch
{
    public static bool Matches(string query, string? text)
    {
        if (string.IsNullOrEmpty(text)) return false;
        var q = Normalize(query);
        if (q.Length == 0) return true;
        var t = Normalize(text);
        if (t.Contains(q, StringComparison.Ordinal)) return true;
        return IsSubsequence(q, t);
    }

    private static string Normalize(string s)
    {
        var formD = s.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(formD.Length);
        foreach (var c in formD)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
            if (c == ' ') continue;
            sb.Append(c);
        }
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    private static bool IsSubsequence(string needle, string haystack)
    {
        if (needle.Length == 0) return true;
        var needleIdx = 0;
        foreach (var ch in haystack)
        {
            if (ch == needle[needleIdx])
            {
                needleIdx++;
                if (needleIdx == needle.Length) return true;
            }
        }
        return false;
    }
}

/// Căutare globală pe un produs — oglinda extensiei VaultEntry.matchesSearch
/// (Mac). Nume, URL login, Notițe, Resurse și TOATE asset-urile cumpărate
/// (nume/serie/link/folder). Secretele reale (parolă, serie A PRODUSULUI)
/// rămân în DPAPI, necăutabile aici intenționat — cheile de serie ale
/// asset-urilor cumpărate SUNT în clar (`PurchasedAsset.LicenseKey`, nu
/// e un secret DPAPI) și intră în căutare.
public static class VaultEntrySearchExtensions
{
    public static bool MatchesSearch(this VaultEntry entry, string query)
    {
        var trimmed = query.Trim();
        if (trimmed.Length == 0) return true;

        if (FuzzySearch.Matches(trimmed, entry.Name)) return true;
        if (FuzzySearch.Matches(trimmed, entry.LoginUrl)) return true;
        if (FuzzySearch.Matches(trimmed, entry.Notes)) return true;
        if (FuzzySearch.Matches(trimmed, entry.DownloadUrl)) return true;
        if (FuzzySearch.Matches(trimmed, entry.UpdateUrl)) return true;

        foreach (var asset in entry.PurchasedAssets)
        {
            if (FuzzySearch.Matches(trimmed, asset.Name)) return true;
            if (FuzzySearch.Matches(trimmed, asset.LicenseKey)) return true;
            if (FuzzySearch.Matches(trimmed, asset.DownloadUrl)) return true;
            if (FuzzySearch.Matches(trimmed, asset.FolderPath)) return true;
        }
        return false;
    }
}
