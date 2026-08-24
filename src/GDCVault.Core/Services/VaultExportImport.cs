using System.Security.Cryptography;
using System.Text.Json;
using GDCVault.Core.Models;

namespace GDCVault.Core.Services;

/// Oglinda VaultExportImport.swift (Mac) — ACELASI format de fisier
/// (".gdcvault"): [8B magic "GDCVLT1\0"][16B salt][AES-GCM: 12B nonce +
/// ciphertext + 16B tag], plaintext = JSON cu toate intrarile, secretele
/// lor si atasamentele ca base64 inline. Un backup exportat de pe Mac
/// trebuie sa se importe neschimbat pe Windows si invers — de-aia
/// parametrii PBKDF2 (200k iteratii, cheie 32 bytes) si formatul JSON
/// (camelCase) sunt tinuti identici intre cele doua parti.
///
/// Spre deosebire de Mac (unde am scris PBKDF2 manual peste CryptoKit),
/// aici .NET are Rfc2898DeriveBytes si AesGcm native — nicio implementare
/// proprie de criptografie necesara.
public static class VaultExportImport
{
    private static readonly byte[] Magic = "GDCVLT1\0"u8.ToArray(); // 8 bytes exact
    private const int SaltSize = 16;
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private const int KeySize = 32;
    private const int Iterations = 200_000;

    public sealed class WrongPasswordException : Exception { }
    public sealed class CorruptFileException : Exception { }

    private sealed class ExportAttachment
    {
        public AttachmentRef Ref { get; set; } = new();
        public string DataBase64 { get; set; } = "";
    }

    private sealed class ExportEntry
    {
        public VaultEntry Entry { get; set; } = new();
        public string? Secret { get; set; }
        public List<ExportAttachment> Attachments { get; set; } = new();
    }

    private sealed class ExportBundle
    {
        public int FormatVersion { get; set; } = 1;
        public DateTimeOffset ExportedAt { get; set; } = DateTimeOffset.Now;
        public List<ExportEntry> Entries { get; set; } = new();
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    // MARK: - Export

    public static void Export(string filePath, string password, IReadOnlyList<VaultEntry> entries)
    {
        var bundle = new ExportBundle();
        foreach (var entry in entries)
        {
            var secret = entry.HasSecret ? VaultDpapiStore.Read(entry.Id) : null;

            var attachments = new List<ExportAttachment>();
            foreach (var reference in entry.Attachments)
            {
                var path = AttachmentStore.FilePath(reference, entry.Id);
                var data = File.ReadAllBytes(path);
                attachments.Add(new ExportAttachment { Ref = reference, DataBase64 = Convert.ToBase64String(data) });
            }

            bundle.Entries.Add(new ExportEntry { Entry = entry, Secret = secret, Attachments = attachments });
        }

        var plaintext = JsonSerializer.SerializeToUtf8Bytes(bundle, JsonOptions);

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, KeySize);

        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[TagSize];

        using (var aes = new AesGcm(key, TagSize))
        {
            aes.Encrypt(nonce, plaintext, ciphertext, tag);
        }

        using var output = File.Create(filePath);
        output.Write(Magic);
        output.Write(salt);
        output.Write(nonce);
        output.Write(ciphertext);
        output.Write(tag);
    }

    // MARK: - Import

    /// Decripteaza si scrie DIRECT in `store`/DPAPI/AttachmentStore —
    /// niciun secret nu iese din aceasta metoda in clar.
    public static void ImportBundle(string filePath, string password, VaultMetadataStore store)
    {
        var raw = File.ReadAllBytes(filePath);
        if (raw.Length <= Magic.Length + SaltSize + NonceSize + TagSize || !raw.AsSpan(0, Magic.Length).SequenceEqual(Magic))
        {
            throw new CorruptFileException();
        }

        var offset = Magic.Length;
        var salt = raw.AsSpan(offset, SaltSize).ToArray(); offset += SaltSize;
        var nonce = raw.AsSpan(offset, NonceSize).ToArray(); offset += NonceSize;
        var ciphertext = raw.AsSpan(offset, raw.Length - offset - TagSize).ToArray();
        var tag = raw.AsSpan(raw.Length - TagSize, TagSize).ToArray();

        var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, KeySize);
        var plaintext = new byte[ciphertext.Length];

        try
        {
            using var aes = new AesGcm(key, TagSize);
            aes.Decrypt(nonce, ciphertext, tag, plaintext);
        }
        catch (CryptographicException)
        {
            // AES-GCM autentifica ciphertext-ul: o parola gresita produce
            // un tag invalid si Decrypt arunca - nu descifreaza gunoi.
            throw new WrongPasswordException();
        }

        var bundle = JsonSerializer.Deserialize<ExportBundle>(plaintext, JsonOptions)
            ?? throw new CorruptFileException();

        foreach (var exportEntry in bundle.Entries)
        {
            var entry = exportEntry.Entry;
            if (exportEntry.Secret is not null)
            {
                VaultDpapiStore.Save(exportEntry.Secret, entry.Id);
                entry.HasSecret = true;
            }

            var restored = new List<AttachmentRef>();
            foreach (var attachment in exportEntry.Attachments)
            {
                var data = Convert.FromBase64String(attachment.DataBase64);
                var dir = AttachmentStore.Directory(entry.Id);
                var destination = Path.Combine(dir, attachment.Ref.StoredFileName);
                File.WriteAllBytes(destination, data);
                restored.Add(attachment.Ref);
            }
            entry.Attachments = restored;

            store.Upsert(entry);
        }
    }
}
