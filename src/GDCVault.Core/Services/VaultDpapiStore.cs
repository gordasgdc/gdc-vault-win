using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;

namespace GDCVault.Core.Services;

/// Echivalentul Windows al VaultKeychainStore.swift (Mac). DPAPI
/// (`ProtectedData`) in loc de Windows Credential Manager: nu cere un
/// pachet nuget suplimentar, cripteaza/decripteaza cu cheia derivata din
/// contul de Windows curent (`DataProtectionScope.CurrentUser`), si e
/// suficient pentru un fisier per-secret pe disc - simplu de facut backup
/// si de sters idempotent, la fel ca un item Keychain.
///
/// Un secret = un fisier `<id>.bin` in
/// `%LOCALAPPDATA%\GDC Vault\secrets\`, continand bytes-ii criptati.
/// Fisierul e inutil oricui in afara acestui cont Windows - DPAPI leaga
/// decriptarea de userul care a criptat.
[SupportedOSPlatform("windows")]
public static class VaultDpapiStore
{
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

    private static string PathFor(Guid id) => Path.Combine(SecretsDir, $"{id}.bin");

    public static void Save(string secret, Guid entryId)
    {
        var plain = Encoding.UTF8.GetBytes(secret);
        var encrypted = ProtectedData.Protect(plain, null, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(PathFor(entryId), encrypted);
    }

    public static string? Read(Guid entryId)
    {
        var path = PathFor(entryId);
        if (!File.Exists(path)) return null;
        var encrypted = File.ReadAllBytes(path);
        var plain = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
        return Encoding.UTF8.GetString(plain);
    }

    /// Idempotent: stergerea unei intrari fara secret nu trebuie sa arunce.
    public static void Delete(Guid entryId)
    {
        var path = PathFor(entryId);
        if (File.Exists(path)) File.Delete(path);
    }
}
