using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;

namespace GDCVault.Core.Services;

/// Echivalentul Windows al VaultKeychainStore.swift (Mac). DPAPI
/// (`ProtectedData`) in loc de Windows Credential Manager: nu cere un
/// pachet nuget suplimentar, cripteaza/decripteaza cu cheia derivata din
/// contul de Windows curent (`DataProtectionScope.CurrentUser`).
///
/// PITFALL FIXED 2026-08-24: prima versiune avea UN SINGUR secret per
/// intrare - dar o fisa unificata de produs poate avea AMBELE (parolă +
/// cheie de serie) simultan. Acum fiecare intrare are DOUA fisiere
/// independente: `<id>.password.bin` si `<id>.serial.bin`.
[SupportedOSPlatform("windows")]
public static class VaultDpapiStore
{
    public enum SecretSlot { Password, Serial }

    private static string SecretsDir
    {
        get
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "GDC Vault", "secrets");
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    private static string PathFor(Guid id, SecretSlot slot) =>
        Path.Combine(SecretsDir, $"{id}.{slot.ToString().ToLowerInvariant()}.bin");

    public static void Save(string secret, Guid entryId, SecretSlot slot)
    {
        var plain = Encoding.UTF8.GetBytes(secret);
        var encrypted = ProtectedData.Protect(plain, null, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(PathFor(entryId, slot), encrypted);
    }

    public static string? Read(Guid entryId, SecretSlot slot)
    {
        var path = PathFor(entryId, slot);
        if (!File.Exists(path)) return null;
        var encrypted = File.ReadAllBytes(path);
        var plain = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
        return Encoding.UTF8.GetString(plain);
    }

    /// Idempotent: stergerea unei intrari fara secret nu trebuie sa arunce.
    public static void Delete(Guid entryId, SecretSlot slot)
    {
        var path = PathFor(entryId, slot);
        if (File.Exists(path)) File.Delete(path);
    }

    /// Sterge ambele sloturi (parola + serial) - apelat la stergerea
    /// intregii intrari din Vault.
    public static void DeleteAll(Guid entryId)
    {
        Delete(entryId, SecretSlot.Password);
        Delete(entryId, SecretSlot.Serial);
    }
}
