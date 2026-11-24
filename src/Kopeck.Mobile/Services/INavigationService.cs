namespace Kopeck.Mobile.Services;

/// <summary>Navigation abstraction so view models never touch <c>Shell.Current</c> (keeps them unit-testable).</summary>
public interface INavigationService
{
    Task GoToAsync(string route);
    Task BackAsync();
}
