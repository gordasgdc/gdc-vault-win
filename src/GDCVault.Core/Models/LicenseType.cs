namespace GDCVault.Core.Models;

/// Oglinda LicenseType.swift (Mac) - vezi comentariile de acolo pentru
/// rationamentul complet (fisa unificata de produs, 2026-08-24).
public enum LicenseType
{
    None,          // doar cont/resurse urmarite, fara licentiere
    Perpetual,     // cumparat definitiv
    Subscription   // abonament recurent
}

public static class LicenseTypeExtensions
{
    public static string DisplayName(this LicenseType type) => type switch
    {
        LicenseType.None => "Fără licențiere (doar cont/resurse)",
        LicenseType.Perpetual => "Cumpărat definitiv",
        LicenseType.Subscription => "Abonament",
        _ => type.ToString()
    };
}
