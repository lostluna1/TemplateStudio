using Microsoft.UI.Xaml.Controls;

namespace Param_RootNamespace.Contracts.Services;

public interface INavigationService
{
    event EventHandler<NavigationEventArgs>? Navigated;

    bool NavigateTo(string pageKey, object? parameter = null);

    void NavigateToInNewTab(string pageKey, object? parameter = null);

    void CloseTab(object tabItem);

    void SwitchToTab(object tabItem);

    Frame? GetOrCreateTabFrame(object tabItem);
}

public class NavigationEventArgs : EventArgs
{
    public Type? SourcePageType { get; set; }
    public object? Parameter { get; set; }
    public Frame? Frame { get; set; }
}
