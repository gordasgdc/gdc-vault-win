namespace GDCVault.Core.Models;

/// Oglinda VaultEntryKind.swift (Mac) - trebuie sa ramana sincronizate,
/// pentru ca metadatele nu circula intre platforme (fiecare masina isi
/// tine propriul entries.json), dar vrem acelasi vocabular in UI.
public enum VaultEntryKind
{
    PerpetualLicense, // cumparat definitiv, cu/fara serial
    Subscription,      // abonament recurent, cu data de reinnoire
    Credential         // user/parola pentru un site/serviciu
}

public static class VaultEntryKindExtensions
{
    public static string DisplayName(this VaultEntryKind kind) => kind switch
    {
        VaultEntryKind.PerpetualLicense => "Licență (cumpărat definitiv)",
        VaultEntryKind.Subscription => "Abonament",
        VaultEntryKind.Credential => "Cont / credențiale",
        _ => kind.ToString()
    };
}
