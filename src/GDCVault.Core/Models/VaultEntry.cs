namespace GDCVault.Core.Models;

/// Oglinda VaultEntry.swift (Mac). O intrare = UN PRODUS/APLICAȚIE, cu
/// credențiale + licențiere + resurse pe ACEEAȘI fișă (nu 3 tipuri
/// exclusive - vezi nota de arhitectură 2026-08-24 din partea Mac).
/// Secretele (parolă, serial) sunt DOUĂ sloturi independente în DPAPI
/// (vezi VaultDpapiStore.SecretSlot) — un produs poate avea ambele, una
/// singură, sau niciuna.
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
