namespace GDCVault.Core.Models;

/// Oglinda VaultEntry.swift (Mac) - vezi comentariile de acolo pentru
/// rationamentul complet. NU contine niciun secret in clar; `HasSecret`
/// doar semnaleaza ca exista o parola/serie salvata via DPAPI, gasita
/// dupa `Id` (vezi VaultDpapiStore).
public sealed class VaultEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public VaultEntryKind Kind { get; set; } = VaultEntryKind.PerpetualLicense;
    public string Name { get; set; } = "";
    public DateTimeOffset? ExpiresAt { get; set; }
    public string? DownloadUrl { get; set; }
    public string? UpdateUrl { get; set; }
    public string? LoginUrl { get; set; }
    public string? Username { get; set; }
    public string? Notes { get; set; }

    /// Contracte/facturi/screenshot-uri legate de aceasta intrare - doar
    /// referinte (vezi AttachmentRef), bytes-ii fisierelor stau pe disc
    /// via AttachmentStore, niciodata in acest JSON.
    public List<AttachmentRef> Attachments { get; set; } = new();

    public bool HasSecret { get; set; }

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
