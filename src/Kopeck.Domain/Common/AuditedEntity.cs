namespace Kopeck.Domain.Common;

/// <summary>
/// An entry that remembers who added it and when, and who last changed it and when. Times are UTC.
/// <see cref="UpdatedAt"/> stays empty until the first real change. The full list of changes is kept in
/// <see cref="History.EntryChange"/>.
/// </summary>
public abstract class AuditedEntity
{
    public DateTime CreatedAt { get; private set; }
    public Guid? CreatedByUserId { get; private set; }
    public string? CreatedByName { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public Guid? UpdatedByUserId { get; private set; }
    public string? UpdatedByName { get; private set; }

    public void MarkCreated(Actor by, DateTime utcNow) => (CreatedAt, CreatedByUserId, CreatedByName) = (utcNow, by.UserId, by.Name);

    public void MarkUpdated(Actor by, DateTime utcNow) => (UpdatedAt, UpdatedByUserId, UpdatedByName) = (utcNow, by.UserId, by.Name);
}
