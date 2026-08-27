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
}
