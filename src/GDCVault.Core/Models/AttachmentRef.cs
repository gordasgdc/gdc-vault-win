namespace GDCVault.Core.Models;

/// Oglinda AttachmentRef.swift (Mac). Fisierul real NU sta aici - doar
/// numele original (de afisat) si numele stocat pe disc (id + extensie),
/// vezi AttachmentStore.
public sealed class AttachmentRef
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string OriginalFileName { get; set; } = "";
    public string StoredFileName { get; set; } = "";
    public DateTimeOffset AddedAt { get; set; } = DateTimeOffset.Now;
}
