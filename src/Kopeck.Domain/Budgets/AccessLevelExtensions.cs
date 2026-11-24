namespace Kopeck.Domain.Budgets;

public static class AccessLevelExtensions
{
    /// <summary>Ownership can't be shared; only Viewer and Editor can be granted.</summary>
    public static bool IsShareable(this AccessLevel level) => level is AccessLevel.Viewer or AccessLevel.Editor;
}
