using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Param_RootNamespace.Contracts.Services;
using Param_RootNamespace.Helpers;
using Param_RootNamespace.ViewModels;

namespace Param_RootNamespace.Views;

public sealed partial class ShellPage : Page
{
    public ShellViewModel ViewModel
    {
        get;
    }

    private readonly IPageService _pageService;
    private TabViewItem? _contextMenuTab;

    public ShellPage(ShellViewModel viewModel, IPageService pageService)
    {
        ViewModel = viewModel;
        _pageService = pageService;
        InitializeComponent();

        ViewModel.NavigationService.Navigated += OnNavigated;
        Loaded += ShellPage_Loaded;

        App.MainWindow.ExtendsContentIntoTitleBar = true;
        App.MainWindow.SetTitleBar(AppTitleBar);
        App.MainWindow.Activated += MainWindow_Activated;
        AppTitleBarText.Text = "AppDisplayName".GetLocalized();
    }

    private async void ShellPage_Loaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.NavigationViewService.InitializeAsync(NavigationViewControl);
    }

    private void OnNavigated(object? sender, NavigationEventArgs e)
    {
        if (e.SourcePageType == null) return;

        var pageType = e.SourcePageType;

        // Check if a tab with this page type already exists
        foreach (TabViewItem existingTab in TabViewControl.TabItems)
        {
            if (existingTab.Content is Frame frame && frame.Content?.GetType() == pageType)
            {
                // Switch to existing tab
                TabViewControl.SelectedItem = existingTab;
                UpdateNavigationViewSelection(pageType);
                return;
            }
        }

        // Create new tab
        var pageTitle = GetPageTitle(pageType);
        var newTab = new TabViewItem
        {
            Header = pageTitle,
            IconSource = new SymbolIconSource { Symbol = Symbol.Document }
        };

        var newFrame = ViewModel.NavigationService.GetOrCreateTabFrame(newTab);
        if (newFrame != null)
        {
            newFrame.Navigate(pageType, e.Parameter);
            newFrame.Margin = new Thickness(12);
            newTab.Content = newFrame;
        }

        TabViewControl.TabItems.Add(newTab);
        TabViewControl.SelectedItem = newTab;
        UpdateNavigationViewSelection(pageType);
    }

    private void UpdateNavigationViewSelection(Type pageType)
    {
        var selectedItem = ViewModel.NavigationViewService.GetSelectedItem(pageType);
        if (selectedItem != null)
        {
            NavigationViewControl.SelectedItem = selectedItem;
        }
        else if (pageType.Name == "SettingsPage")
        {
            NavigationViewControl.SelectedItem = NavigationViewControl.SettingsItem;
        }
    }

    private string GetPageTitle(Type pageType)
    {
        return pageType.Name.Replace("Page", "").Replace("ViewModel", "");
    }

    private void TabView_AddTabButtonClick(TabView sender, object args)
    {
        ViewModel.NavigationService.NavigateToInNewTab(typeof(MainViewModel).FullName!);
    }

    private void TabView_TabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args)
    {
        if (args.Tab is TabViewItem tab)
        {
            CloseTab(tab);
        }
    }

    private void TabView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (TabViewControl.SelectedItem is TabViewItem selectedTab)
        {
            ViewModel.NavigationService.SwitchToTab(selectedTab);

            if (selectedTab.Content is Frame frame && frame.Content != null)
            {
                var pageType = frame.Content.GetType();
                UpdateNavigationViewSelection(pageType);
            }
        }
    }

    private void TabView_TabItemsChanged(TabView sender, Windows.Foundation.Collections.IVectorChangedEventArgs args)
    {
        if (args.CollectionChange == Windows.Foundation.Collections.CollectionChange.ItemInserted)
        {
            var newTab = sender.TabItems[(int)args.Index] as TabViewItem;
            if (newTab != null)
            {
                newTab.ContextFlyout = Resources["TabContextMenu"] as MenuFlyout;
                newTab.RightTapped += OnTabRightTapped;
            }
        }
    }

    private void OnTabRightTapped(object sender, Microsoft.UI.Xaml.Input.RightTappedRoutedEventArgs e)
    {
        if (sender is TabViewItem tab)
        {
            _contextMenuTab = tab;
        }
    }

    private void CloseCurrentTab_Click(object sender, RoutedEventArgs e)
    {
        if (_contextMenuTab != null && TabViewControl.TabItems.Contains(_contextMenuTab))
        {
            CloseTab(_contextMenuTab);
        }
    }

    private void CloseTab(TabViewItem tab)
    {
        if (tab.Content is Frame frame)
        {
            frame.Content = null;
        }

        tab.RightTapped -= OnTabRightTapped;
        tab.Content = null;

        TabViewControl.TabItems.Remove(tab);
        ViewModel.NavigationService.CloseTab(tab);
    }

    private void CloseLeftTabs_Click(object sender, RoutedEventArgs e)
    {
        if (_contextMenuTab == null) return;

        var index = TabViewControl.TabItems.IndexOf(_contextMenuTab);
        if (index <= 0) return;

        for (int i = index - 1; i >= 0; i--)
        {
            var tab = TabViewControl.TabItems[i] as TabViewItem;
            if (tab != null)
            {
                CloseTab(tab);
            }
        }
    }

    private void CloseRightTabs_Click(object sender, RoutedEventArgs e)
    {
        if (_contextMenuTab == null) return;

        var index = TabViewControl.TabItems.IndexOf(_contextMenuTab);
        if (index < 0 || index >= TabViewControl.TabItems.Count - 1) return;

        for (int i = TabViewControl.TabItems.Count - 1; i > index; i--)
        {
            var tab = TabViewControl.TabItems[i] as TabViewItem;
            if (tab != null)
            {
                CloseTab(tab);
            }
        }
    }

    private void CloseAllTabs_Click(object sender, RoutedEventArgs e)
    {
        while (TabViewControl.TabItems.Count > 0)
        {
            var tab = TabViewControl.TabItems[0] as TabViewItem;
            if (tab != null)
            {
                CloseTab(tab);
            }
        }
    }

    private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
    {
        var resource = args.WindowActivationState == WindowActivationState.Deactivated
            ? "WindowCaptionForegroundDisabled"
            : "WindowCaptionForeground";

        AppTitleBarText.Foreground = (Microsoft.UI.Xaml.Media.Brush)App.Current.Resources[resource];
    }

    private void NavigationViewControl_DisplayModeChanged(NavigationView sender, NavigationViewDisplayModeChangedEventArgs args)
    {
        AppTitleBar.Margin = new Thickness()
        {
            Left = sender.CompactPaneLength * (sender.DisplayMode == NavigationViewDisplayMode.Minimal ? 2 : 1),
            Top = AppTitleBar.Margin.Top,
            Right = AppTitleBar.Margin.Right,
            Bottom = AppTitleBar.Margin.Bottom
        };
    }
}
