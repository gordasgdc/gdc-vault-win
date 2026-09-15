using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GDCVault.Core.Models;

namespace GDCVault.Core.Services;

/// Copii de siguranta automate, criptate, ale listei de intrari.
/// Port 1:1 al AutoBackupService.swift.
///
/// CE CONTINE SI CE NU: aceleasi metadate ca `entries.json`. NU contine
/// parole si chei de serie — acelea stau in DPAPI si raman acolo. O copie
/// pierduta nu dezvaluie niciun secret.
///
/// Cheia e generata o data, la intamplare, si protejata cu DPAPI legat de
/// utilizatorul curent. Deliberat NU derivata din id-ul masinii: aceea ar fi
/// fost o "parola" publica, citibila de oricine de pe acelasi calculator.
public static class AutoBackupService
{
    private static readonly byte[] Magic = Encoding.ASCII.GetBytes("GDCBAK1\0");
    private const int KeepCount = 10;
    private const int NonceSize = 12;   // AES-GCM standard
    private const int TagSize = 16;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static string BackupsDirectory
    {
        get
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "GDCVault", "Backups");
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    // ─── Cheia ──────────────────────────────────────────────────────────

    private static string KeyPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "GDCVault", "backup.key");

    private static byte[] BackupKey()
    {
        if (File.Exists(KeyPath))
        {
            try
            {
                var protectedKey = File.ReadAllBytes(KeyPath);
                var key = ProtectedData.Unprotect(protectedKey, null, DataProtectionScope.CurrentUser);
                if (key.Length == 32) return key;
            }
            catch
            {
                // Cheie corupta sau scrisa de alt utilizator Windows: o
                // regeneram. Copiile vechi devin necitibile, dar alternativa
                // ar fi sa nu mai putem face NICIUNA de acum inainte.
            }
        }

        var fresh = RandomNumberGenerator.GetBytes(32);
        Directory.CreateDirectory(Path.GetDirectoryName(KeyPath)!);
        File.WriteAllBytes(KeyPath, ProtectedData.Protect(fresh, null, DataProtectionScope.CurrentUser));
        return fresh;
    }

    // ─── Scriere ────────────────────────────────────────────────────────

    /// Un seif GOL nu se salveaza niciodata: altfel, o pornire in care datele
    /// n-au putut fi citite ar produce o copie goala care, peste cateva
    /// salvari, le-ar impinge afara pe cele bune — exact scenariul impotriva
    /// caruia exista acest modul.
    public static string? Backup(IReadOnlyList<VaultEntry> entries)
    {
        if (entries is null || entries.Count == 0) return null;

        try
        {
            var plaintext = JsonSerializer.SerializeToUtf8Bytes(entries, JsonOptions);

            var nonce = RandomNumberGenerator.GetBytes(NonceSize);
            var ciphertext = new byte[plaintext.Length];
            var tag = new byte[TagSize];
            using (var aes = new AesGcm(BackupKey(), TagSize))
            {
                aes.Encrypt(nonce, plaintext, ciphertext, tag);
            }

            // Acelasi asezare ca pe Mac (`SealedBox.combined`):
            // magic + nonce + ciphertext + tag.
            var path = Path.Combine(BackupsDirectory,
                $"vault_backup_{DateTime.Now:yyyyMMdd_HHmmss}.enc");
            using (var output = File.Create(path))
            {
                output.Write(Magic);
                output.Write(nonce);
                output.Write(ciphertext);
                output.Write(tag);
            }

            Prune();
            return path;
        }
        catch
        {
            // O copie esuata nu are voie sa opreasca salvarea reala a
            // datelor — e o plasa de siguranta, nu o conditie.
            return null;
        }
    }

    /// Copia fisierului de pe DISC, nu a listei din memorie: aceea e deja
    /// starea noua (Upsert modifica lista, apoi cheama Save), iar o copie a
    /// starii noi n-ar folosi la nimic la o revenire.
    public static string? BackupFile(string entriesFilePath)
    {
        if (!File.Exists(entriesFilePath)) return null;
        try
        {
            var json = File.ReadAllText(entriesFilePath);
            var previous = JsonSerializer.Deserialize<List<VaultEntry>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            });
            return previous is null ? null : Backup(previous);
        }
        catch
        {
            // Fisier corupt: nu avem ce salva din el, iar o copie a unui JSON
            // stricat ar ocupa un slot din cele 10 fara sa poata fi restaurata.
            return null;
        }
    }

    private static void Prune()
    {
        var files = BackupFiles();
        foreach (var path in files.Skip(KeepCount)) 
        {
            try { File.Delete(path); } catch { /* fisier blocat: incercam data viitoare */ }
        }
    }

    /// Cea mai NOUA prima. Sortare dupa NUME, nu dupa data fisierului:
    /// formatul `yyyyMMdd_HHmmss` e sortabil alfabetic, iar data de
    /// modificare se poate schimba la o copiere pe alt disc.
    public static List<string> BackupFiles()
    {
        try
        {
            return Directory.GetFiles(BackupsDirectory, "vault_backup_*.enc")
                .OrderByDescending(Path.GetFileName, StringComparer.Ordinal)
                .ToList();
        }
        catch
        {
            return new List<string>();
        }
    }

    // ─── Citire ─────────────────────────────────────────────────────────

    public static List<VaultEntry>? Decode(string path)
    {
        try
        {
            var raw = File.ReadAllBytes(path);
            if (raw.Length <= Magic.Length + NonceSize + TagSize) return null;
            if (!raw.Take(Magic.Length).SequenceEqual(Magic)) return null;

            var nonce = raw.AsSpan(Magic.Length, NonceSize);
            var cipherLength = raw.Length - Magic.Length - NonceSize - TagSize;
            var ciphertext = raw.AsSpan(Magic.Length + NonceSize, cipherLength);
            var tag = raw.AsSpan(Magic.Length + NonceSize + cipherLength, TagSize);

            var plaintext = new byte[cipherLength];
            using (var aes = new AesGcm(BackupKey(), TagSize))
            {
                aes.Decrypt(nonce, ciphertext, tag, plaintext);
            }

            return JsonSerializer.Deserialize<List<VaultEntry>>(plaintext, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            });
        }
        catch
        {
            return null;
        }
    }

    /// Cea mai recenta copie care chiar se decripteaza SI contine intrari.
    /// Se incearca pe rand, nu doar prima: una trunchiata de o inchidere
    /// brusca n-are voie sa blocheze recuperarea din cea dinaintea ei.
    public static (string Path, List<VaultEntry> Entries)? LatestRestorable()
    {
        foreach (var path in BackupFiles())
        {
            var entries = Decode(path);
            if (entries is { Count: > 0 }) return (path, entries);
        }
        return null;
    }
}
