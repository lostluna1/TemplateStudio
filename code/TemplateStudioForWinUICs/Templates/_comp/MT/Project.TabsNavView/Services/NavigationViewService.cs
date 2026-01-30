using System.Diagnostics.CodeAnalysis;
using Microsoft.UI.Xaml.Controls;
using Param_RootNamespace.Contracts.Services;
using Param_RootNamespace.Helpers;
using Param_RootNamespace.Models;

namespace Param_RootNamespace.Services;

public class NavigationViewService : INavigationViewService
{
    private readonly INavigationService _navigationService;
    private readonly IPageService _pageService;
    private readonly INavigationConfigService _navigationConfigService;
    private NavigationView? _navigationView;

    public IList<object>? MenuItems => _navigationView?.MenuItems;

    public object? SettingsItem => _navigationView?.SettingsItem;

    public NavigationViewService(
        INavigationService navigationService,
        IPageService pageService,
        INavigationConfigService navigationConfigService)
    {
        _navigationService = navigationService;
        _pageService = pageService;
        _navigationConfigService = navigationConfigService;
    }

    [MemberNotNull(nameof(_navigationView))]
    public async Task InitializeAsync(NavigationView navigationView)
    {
        _navigationView = navigationView;
        _navigationView.ItemInvoked += OnItemInvoked;

        await LoadNavigationItemsAsync();
    }

    public void UnregisterEvents()
    {
        if (_navigationView != null)
        {
            _navigationView.ItemInvoked -= OnItemInvoked;
        }
    }

    public async Task LoadNavigationItemsAsync()
    {
        if (_navigationView == null) return;

        var items = await _navigationConfigService.LoadNavigationItemsAsync();
        _navigationView.MenuItems.Clear();

        foreach (var item in items)
        {
            AddNavigationItem(_navigationView.MenuItems, item);
        }
    }

    private void AddNavigationItem(IList<object> container, NavigationItem item)
    {
        switch (item.Type.ToLower())
        {
            case "separator":
                container.Add(new NavigationViewItemSeparator());
                break;

            case "header":
                container.Add(new NavigationViewItemHeader { Content = item.Title });
                break;

            case "item":
            default:
                var navItem = new NavigationViewItem
                {
                    Content = item.Title,
                    Tag = item.Id
                };

                if (!string.IsNullOrEmpty(item.Icon))
                {
                    navItem.Icon = new FontIcon
                    {
                        FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe Fluent Icons"),
                        Glyph = item.Icon
                    };
                }

                if (!string.IsNullOrEmpty(item.NavigateTo))
                {
                    NavigationHelper.SetNavigateTo(navItem, item.NavigateTo);
                }

                if (item.Children?.Any() == true)
                {
                    foreach (var child in item.Children)
                    {
                        AddNavigationItem(navItem.MenuItems, child);
                    }
                }

                container.Add(navItem);
                break;
        }
    }

    public NavigationViewItem? GetSelectedItem(Type pageType)
    {
        if (_navigationView != null)
        {
            return GetSelectedItem(_navigationView.MenuItems, pageType)
                ?? GetSelectedItem(_navigationView.FooterMenuItems, pageType);
        }
        return null;
    }

    private void OnItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        if (args.IsSettingsInvoked)
        {
            _navigationService.NavigateToInNewTab(typeof(ViewModels.SettingsViewModel).FullName!);
        }
        else
        {
            var selectedItem = args.InvokedItemContainer as NavigationViewItem;
            if (selectedItem?.GetValue(NavigationHelper.NavigateToProperty) is string pageKey)
            {
                _navigationService.NavigateToInNewTab(pageKey);
            }
        }
    }

    private NavigationViewItem? GetSelectedItem(IEnumerable<object> menuItems, Type pageType)
    {
        foreach (var item in menuItems.OfType<NavigationViewItem>())
        {
            if (IsMenuItemForPageType(item, pageType))
            {
                return item;
            }

            var selectedChild = GetSelectedItem(item.MenuItems, pageType);
            if (selectedChild != null)
            {
                return selectedChild;
            }
        }
        return null;
    }

    private bool IsMenuItemForPageType(NavigationViewItem menuItem, Type sourcePageType)
    {
        if (menuItem.GetValue(NavigationHelper.NavigateToProperty) is string pageKey)
        {
            return _pageService.GetPageType(pageKey) == sourcePageType;
        }
        return false;
    }
}
