using Kopeck.Domain.Common;

namespace Kopeck.Domain.History;

/// <summary>
/// One action on an entry: who added, changed or deleted it, when, and which values changed. Rows are only ever added,
/// never edited, and outlive the entry itself (a deleted expense keeps its history).
/// </summary>
public sealed class EntryChange : IBudgetOwned
{
    private EntryChange() { }   // EF Core

    public EntryChange(Guid budgetId, EntryKind kind, Guid entryId, EntryAction action, Actor by, DateTime utcNow, IReadOnlyList<FieldChange> changes)
    {
        BudgetId = budgetId;
        Kind = kind;
        EntryId = entryId;
        Action = action;
        ByUserId = by.UserId;
        ByName = by.Name;
        At = utcNow;
        Changes = changes;
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid BudgetId { get; private set; }
    public EntryKind Kind { get; private set; }
    public Guid EntryId { get; private set; }
    public EntryAction Action { get; private set; }
    public DateTime At { get; private set; }
    public Guid ByUserId { get; private set; }
    public string ByName { get; private set; } = "";
    public IReadOnlyList<FieldChange> Changes { get; private set; } = [];
}
