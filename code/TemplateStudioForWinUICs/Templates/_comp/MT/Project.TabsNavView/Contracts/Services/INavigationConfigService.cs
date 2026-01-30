namespace Param_RootNamespace.Contracts.Services;

public interface INavigationConfigService
{
    Task<List<Models.NavigationItem>> LoadNavigationItemsAsync();
}
