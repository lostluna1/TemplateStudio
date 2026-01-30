using CommunityToolkit.Mvvm.ComponentModel;
using Param_RootNamespace.Contracts.Services;

namespace Param_RootNamespace.ViewModels;

public partial class ShellViewModel : ObservableRecipient
{
    [ObservableProperty]
    private object? selectedTab;

    public INavigationService NavigationService
    {
        get;
    }

    public INavigationViewService NavigationViewService
    {
        get;
    }

    public ShellViewModel(INavigationService navigationService, INavigationViewService navigationViewService)
    {
        NavigationService = navigationService;
        NavigationService.Navigated += OnNavigated;
        NavigationViewService = navigationViewService;
    }

    private void OnNavigated(object? sender, NavigationEventArgs e)
    {
        // TabView will handle navigation events
    }
}
