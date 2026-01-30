using Microsoft.UI.Xaml.Controls;

namespace Param_RootNamespace.Contracts.Services;

public interface INavigationViewService
{
    IList<object>? MenuItems
    {
        get;
    }

    object? SettingsItem
    {
        get;
    }

    Task InitializeAsync(NavigationView navigationView);

    void UnregisterEvents();

    NavigationViewItem? GetSelectedItem(Type pageType);

    Task LoadNavigationItemsAsync();
}
