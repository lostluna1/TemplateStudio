using Microsoft.UI.Xaml.Controls;
using Param_RootNamespace.Contracts.Services;

namespace Param_RootNamespace.Services;

// For more information on navigation between pages see
// https://github.com/microsoft/TemplateStudio/blob/main/docs/WinUI/navigation.md
public class NavigationService : INavigationService
{
    private readonly IPageService _pageService;
    private readonly Dictionary<object, Frame> _tabFrames = new();
    private object? _currentTab;

    public event EventHandler<NavigationEventArgs>? Navigated;

    public NavigationService(IPageService pageService)
    {
        _pageService = pageService;
    }

    public bool NavigateTo(string pageKey, object? parameter = null)
    {
        if (_currentTab == null)
        {
            NavigateToInNewTab(pageKey, parameter);
            return true;
        }

        var frame = GetOrCreateTabFrame(_currentTab);
        if (frame != null)
        {
            var pageType = _pageService.GetPageType(pageKey);
            var navigated = frame.Navigate(pageType, parameter);
            
            if (navigated)
            {
                Navigated?.Invoke(this, new NavigationEventArgs 
                { 
                    SourcePageType = pageType, 
                    Parameter = parameter,
                    Frame = frame
                });
            }
            
            return navigated;
        }

        return false;
    }

    public void NavigateToInNewTab(string pageKey, object? parameter = null)
    {
        var pageType = _pageService.GetPageType(pageKey);
        Navigated?.Invoke(this, new NavigationEventArgs 
        { 
            SourcePageType = pageType, 
            Parameter = parameter 
        });
    }

    public void CloseTab(object tabItem)
    {
        if (_tabFrames.ContainsKey(tabItem))
        {
            _tabFrames.Remove(tabItem);
        }
    }

    public void SwitchToTab(object tabItem)
    {
        _currentTab = tabItem;
    }

    public Frame? GetOrCreateTabFrame(object tabItem)
    {
        if (!_tabFrames.ContainsKey(tabItem))
        {
            _tabFrames[tabItem] = new Frame();
        }
        return _tabFrames[tabItem];
    }
}
