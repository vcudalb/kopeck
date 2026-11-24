namespace Kopeck.Domain.Budgets;

/// <summary>What a person may do with a budget. Ordered, so "at least Editor" is <c>level &gt;= Editor</c>.</summary>
public enum AccessLevel
{
    Viewer = 0,
    Editor = 1,
    Owner = 2,
}
