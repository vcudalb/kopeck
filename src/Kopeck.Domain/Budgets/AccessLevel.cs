namespace Kopeck.Domain.Budgets;

/// <summary>What a person may do with a budget. Ordered, so "at least Editor" is <c>level &gt;= Editor</c>.</summary>
public enum AccessLevel
{
    Viewer = 0,
    Editor = 1,
    Owner = 2,
}

public static class AccessLevelExtensions
{
    /// <summary>Ownership can't be shared; only Viewer and Editor can be granted.</summary>
    public static bool IsShareable(this AccessLevel level) => level is AccessLevel.Viewer or AccessLevel.Editor;
}
