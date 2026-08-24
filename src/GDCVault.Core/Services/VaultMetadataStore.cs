using System.Text.Json;
using GDCVault.Core.Models;

namespace GDCVault.Core.Services;

/// Oglinda VaultMetadataStore.swift (Mac): JSON simplu, FARA secrete, in
/// `%LOCALAPPDATA%\GDC Vault\entries.json`.
public sealed class VaultMetadataStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly string _filePath;

    public List<VaultEntry> Entries { get; private set; } = new();

    public VaultMetadataStore()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "GDC Vault");
        Directory.CreateDirectory(dir);
        _filePath = Path.Combine(dir, "entries.json");
        Load();
    }

    private void Load()
    {
        if (!File.Exists(_filePath)) return;
        try
        {
            var json = File.ReadAllText(_filePath);
            Entries = JsonSerializer.Deserialize<List<VaultEntry>>(json, JsonOptions) ?? new();
        }
        catch
        {
            // Fisier corupt/lipsa - pornim cu lista goala in loc sa picam aplicatia.
            Entries = new();
        }
    }

    private void Save()
    {
        var json = JsonSerializer.Serialize(Entries, JsonOptions);
        File.WriteAllText(_filePath, json);
    }

    public void Upsert(VaultEntry entry)
    {
        var idx = Entries.FindIndex(e => e.Id == entry.Id);
        if (idx >= 0) Entries[idx] = entry;
        else Entries.Add(entry);
        Save();
    }

    /// Sterge intrarea SI secretul DPAPI asociat - altfel ramane un
    /// fisier `.bin` orfan in `secrets\`.
    public void Delete(VaultEntry entry)
    {
        Entries.RemoveAll(e => e.Id == entry.Id);
        VaultDpapiStore.Delete(entry.Id);
        AttachmentStore.RemoveAll(entry.Id);
        Save();
    }

    public List<VaultEntry> ExpiringSoon(int withinDays = 14) =>
        Entries.Where(e => e.DaysUntilExpiry is int d && d <= withinDays)
               .OrderBy(e => e.ExpiresAt ?? DateTimeOffset.MaxValue)
               .ToList();
}
