using System.Globalization;
using Kopeck.Application.Abstractions;
using Kopeck.Domain.Common;
using Kopeck.Domain.History;

namespace Kopeck.Application.History;

/// <summary>
/// Stamps who added or changed an entry and when, and records the action with its values in the entry's history.
/// Call it before <c>SaveChangesAsync</c>, so the entry and its history row are saved together.
/// </summary>
public sealed class EntryAudit(IKopeckDbContext db, ICurrentUser user, TimeProvider clock)
{
    private Actor Me => new(user.Id, user.DisplayName);
    private DateTime Now => clock.GetUtcNow().UtcDateTime;

    public void Created(AuditedEntity entry, Guid budgetId, EntryKind kind, Guid entryId, IReadOnlyList<(string Field, string? Value)> values)
    {
        entry.MarkCreated(Me, Now);
        db.EntryChanges.Add(new EntryChange(budgetId, kind, entryId, EntryAction.Created, Me, Now,
            values.Where(v => v.Value is not null).Select(v => new FieldChange(v.Field, null, v.Value)).ToList()));
    }

    /// <summary>Records only the fields whose value changed. Returns false (and stamps nothing) when nothing changed.</summary>
    public bool Updated(AuditedEntity entry, Guid budgetId, EntryKind kind, Guid entryId,
        IReadOnlyList<(string Field, string? Value)> before, IReadOnlyList<(string Field, string? Value)> after)
    {
        var changes = before.Zip(after, (b, a) => new FieldChange(b.Field, b.Value, a.Value)).Where(c => c.Old != c.New).ToList();
        if (changes.Count == 0) return false;

        entry.MarkUpdated(Me, Now);
        db.EntryChanges.Add(new EntryChange(budgetId, kind, entryId, EntryAction.Updated, Me, Now, changes));
        return true;
    }

    public void Deleted(Guid budgetId, EntryKind kind, Guid entryId, IReadOnlyList<(string Field, string? Value)> values) =>
        db.EntryChanges.Add(new EntryChange(budgetId, kind, entryId, EntryAction.Deleted, Me, Now,
            values.Where(v => v.Value is not null).Select(v => new FieldChange(v.Field, v.Value, null)).ToList()));

    // the text form history values are stored in
    public static string Amount(decimal amount) => amount.ToString("0.00", CultureInfo.InvariantCulture);
    public static string Date(DateOnly date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}
