using GDCVault.Core.Models;

namespace GDCVault.Core.Services;

/// Oglinda AttachmentStore.swift (Mac): un folder per intrare in
/// `%LOCALAPPDATA%\GDC Vault\Attachments\<entryId>\`.
public static class AttachmentStore
{
    public static string Directory(Guid entryId)
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "GDC Vault", "Attachments", entryId.ToString());
        System.IO.Directory.CreateDirectory(dir);
        return dir;
    }

    /// Copiaza `sourcePath` in folderul intrarii si intoarce referinta
    /// gata de adaugat in `VaultEntry.Attachments`. Copiem, nu mutam -
    /// sursa poate fi orice fisier ales prin OpenFileDialog.
    public static AttachmentRef Add(string sourcePath, Guid entryId)
    {
        var dir = Directory(entryId);
        var id = Guid.NewGuid();
        var ext = Path.GetExtension(sourcePath);
        var storedName = string.IsNullOrEmpty(ext) ? id.ToString() : $"{id}{ext}";
        var destination = Path.Combine(dir, storedName);
        File.Copy(sourcePath, destination, overwrite: false);

        return new AttachmentRef
        {
            Id = id,
            OriginalFileName = Path.GetFileName(sourcePath),
            StoredFileName = storedName
        };
    }

    public static string FilePath(AttachmentRef attachment, Guid entryId) =>
        Path.Combine(Directory(entryId), attachment.StoredFileName);

    /// Idempotent: stergerea unui atasament deja disparut de pe disc nu
    /// trebuie sa arunce.
    public static void Remove(AttachmentRef attachment, Guid entryId)
    {
        var path = FilePath(attachment, entryId);
        if (File.Exists(path)) File.Delete(path);
    }

    /// Sterge tot folderul unei intrari - apelat cand intrarea insasi
    /// se sterge din Vault, ca sa nu ramana atasamente orfane.
    public static void RemoveAll(Guid entryId)
    {
        var dir = Directory(entryId);
        if (System.IO.Directory.Exists(dir)) System.IO.Directory.Delete(dir, recursive: true);
    }
}
