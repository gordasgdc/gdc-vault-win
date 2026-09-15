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

    /// Copia gasita la pornire cand fisierul principal lipsea sau era
    /// necitibil. UI-ul o foloseste ca sa OFERE restaurarea — nu restauram
    /// automat: o suprascriere tacuta a datelor e exact ce nu vrei sa faca
    /// o aplicatie de tip seif.
    public (string Path, List<VaultEntry> Entries)? RecoverableBackup { get; private set; }

    public VaultMetadataStore()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "GDC Vault");
        Directory.CreateDirectory(dir);
        _filePath = Path.Combine(dir, "entries.json");
        Load();

        // Copie la fiecare pornire, cu datele deja incarcate. Daca lipsesc,
        // Backup nu scrie nimic (vezi AutoBackupService).
        AutoBackupService.Backup(Entries);
    }

    private void Load()
    {
        try
        {
            if (File.Exists(_filePath))
            {
                var json = File.ReadAllText(_filePath);
                var decoded = JsonSerializer.Deserialize<List<VaultEntry>>(json, JsonOptions);
                if (decoded is { Count: > 0 })
                {
                    Entries = decoded;
                    return;
                }
            }
        }
        catch
        {
            // Fisier corupt - cade mai jos, la cautarea unei copii bune.
        }

        // Fisier lipsa, gol sau necitibil (mutare pe alt disc, folder nou,
        // JSON trunchiat de o inchidere brusca).
        Entries = new();
        RecoverableBackup = AutoBackupService.LatestRestorable();
    }

    /// Aplica o copie gasita la pornire. Inainte de suprascriere face inca o
    /// copie a starii curente — daca restaurarea e o greseala, se poate
    /// intoarce.
    public void Restore((string Path, List<VaultEntry> Entries) backup)
    {
        AutoBackupService.Backup(Entries);
        Entries = backup.Entries;
        RecoverableBackup = null;
        Save();
    }

    public void DismissRecovery() => RecoverableBackup = null;

    private void Save()
    {
        // Copie INAINTE de scriere, din ce e ACUM PE DISC — nu din `Entries`,
        // care e deja starea noua.
        AutoBackupService.BackupFile(_filePath);

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
        VaultDpapiStore.DeleteAll(entry.Id);
        AttachmentStore.RemoveAll(entry.Id);
        Save();
    }

    public List<VaultEntry> ExpiringSoon(int withinDays = 14) =>
        Entries.Where(e => e.DaysUntilExpiry is int d && d <= withinDays)
               .OrderBy(e => e.ExpiresAt ?? DateTimeOffset.MaxValue)
               .ToList();
}
