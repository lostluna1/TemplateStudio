using System.Text.Json;
using Param_RootNamespace.Contracts.Services;
using Param_RootNamespace.Models;

namespace Param_RootNamespace.Services;

public class NavigationConfigService : INavigationConfigService
{
    private const string ConfigFileName = "NavigationConfig.json";

    public async Task<List<NavigationItem>> LoadNavigationItemsAsync()
    {
        try
        {
            var uri = new Uri("ms-appx:///Services/NavigationConfig.json");
            var file = await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(uri);
            var json = await Windows.Storage.FileIO.ReadTextAsync(file);

            // Remove merge markers that template engine leaves
            json = System.Text.RegularExpressions.Regex.Replace(json, @"//\^\^.*?$", "", System.Text.RegularExpressions.RegexOptions.Multiline);
            json = System.Text.RegularExpressions.Regex.Replace(json, @"//\{\[\{.*?$", "", System.Text.RegularExpressions.RegexOptions.Multiline);
            json = System.Text.RegularExpressions.Regex.Replace(json, @"//\}\]\}.*?$", "", System.Text.RegularExpressions.RegexOptions.Multiline);
            json = System.Text.RegularExpressions.Regex.Replace(json, @",\s*\]", "]");
            json = System.Text.RegularExpressions.Regex.Replace(json, @",\s*\}", "}");

            var config = JsonSerializer.Deserialize<NavigationConfig>(json);
            return config?.Items ?? new List<NavigationItem>();
        }
        catch
        {
            return new List<NavigationItem>();
        }
    }
}

internal class NavigationConfig
{
    [System.Text.Json.Serialization.JsonPropertyName("items")]
    public List<NavigationItem> Items { get; set; } = new();
}
