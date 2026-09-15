using System.Globalization;
using System.Text.Json.Serialization;
namespace GDCVault.Core.Models;

/// Oglinda VaultEntry.swift (Mac). O intrare = UN PRODUS/APLICAȚIE, cu
/// credențiale + licențiere + resurse pe ACEEAȘI fișă (nu 3 tipuri
/// exclusive - vezi nota de arhitectură 2026-08-24 din partea Mac).
/// Secretele (parolă, serial) sunt DOUĂ sloturi independente în DPAPI
/// (vezi VaultDpapiStore.SecretSlot) — un produs poate avea ambele, una
/// singură, sau niciuna.
/// Asset/pachet cumpărat de la un furnizor (efecte, SFX, LUT-uri), legat de
/// un folder local — oglinda PurchasedAsset (Mac). Listă dinamică pe
/// VaultEntry.PurchasedAssets (un produs poate avea mai multe).
public sealed class PurchasedAsset
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string? FolderPath { get; set; }
    public string? LicenseKey { get; set; }
    public string? DownloadUrl { get; set; }
}

/// Cont/departament SUPLIMENTAR de login pe același produs (2026-08-27) —
/// oglinda LoginCredential (Mac). Contul PRINCIPAL rămâne LoginUrl/
/// Username/HasPassword direct pe VaultEntry (neschimbat); acestea sunt
/// ADIȚIONALE, listă dinamică. Parola fiecăruia e un fișier DPAPI propriu
/// — vezi VaultDpapiStore.SaveCredentialSecret(entryId, credentialId).
public sealed class LoginCredential
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Label { get; set; } = ""; // "Departament Video", "Cont Facturare"
    public string? LoginUrl { get; set; }
    public string? Username { get; set; }
    public bool HasPassword { get; set; }
}

/// Cat de des se plateste un abonament.
public enum BillingPeriod { Monthly, Yearly }

public sealed class VaultEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";

    // Credențiale
    public string? LoginUrl { get; set; }
    public string? Username { get; set; }
    public bool HasPassword { get; set; }

    // Licențiere
    public LicenseType LicenseType { get; set; } = LicenseType.None;
    public DateTimeOffset? ExpiresAt { get; set; }
    public bool HasSerial { get; set; }

    // Cost (2026-09-15) — optional; null = nu urmarim costul.
    public double? PriceAmount { get; set; }
    public BillingPeriod? BillingPeriod { get; set; }

    // Reminder de reinnoire (2026-09-15). Lista, nu un singur prag: un
    // abonament anual scump merita avertizat din timp SI cu cateva zile
    // inainte, cand chiar actionezi.
    public bool ReminderEnabled { get; set; }
    public List<int> ReminderDaysBefore { get; set; } = new() { 30, 7, 3 };

    // Resurse
    public string? DownloadUrl { get; set; }
    public string? UpdateUrl { get; set; }
    public string? Notes { get; set; }
    public List<AttachmentRef> Attachments { get; set; } = new();

    // Asset-uri cumpărate & foldere locale (2026-08-27). Câmp nou — lipsă în
    // JSON vechi (entries.json) deserializează la default `= new()`, deci
    // intrările existente rămân valide fără migrare.
    public List<PurchasedAsset> PurchasedAssets { get; set; } = new();

    // Conturi/departamente suplimentare (2026-08-27) — vezi LoginCredential.
    public List<LoginCredential> AdditionalLogins { get; set; } = new();

    /// Zile pana la expirare; negativ daca a expirat deja. null = nu expira.
    public int? DaysUntilExpiry
    {
        get
        {
            if (ExpiresAt is null) return null;
            var span = ExpiresAt.Value - DateTimeOffset.Now;
            return (int)Math.Floor(span.TotalDays);
        }
    }

    /// Costul normalizat la o luna, ca totalurile sa fie comparabile.
    [JsonIgnore]
    public double? MonthlyCost =>
        PriceAmount is { } amount && BillingPeriod is { } period
            ? (period == Models.BillingPeriod.Monthly ? amount : amount / 12)
            : null;

    [JsonIgnore]
    public string? PriceDisplay
    {
        get
        {
            if (PriceAmount is not { } amount || BillingPeriod is not { } period) return null;
            var value = Math.Abs(amount % 1) < 0.0001
                ? ((long)amount).ToString(CultureInfo.InvariantCulture)
                : amount.ToString("0.00", CultureInfo.InvariantCulture);
            return $"{value} € / {(period == Models.BillingPeriod.Monthly ? "lunar" : "anual")}";
        }
    }

    /// True daca textul e o adresa web deschizabila.
    ///
    /// Verificarea NU e doar "incepe cu http": un URI sintactic valid dar
    /// fara gazda ("https://") ar deschide o fereastra goala de browser.
    public static bool IsLaunchableUrl(string? text)
    {
        var raw = text?.Trim();
        if (string.IsNullOrEmpty(raw)) return false;
        if (!raw.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            && !raw.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) return false;
        return Uri.TryCreate(raw, UriKind.Absolute, out var uri) && !string.IsNullOrEmpty(uri.Host);
    }
}
