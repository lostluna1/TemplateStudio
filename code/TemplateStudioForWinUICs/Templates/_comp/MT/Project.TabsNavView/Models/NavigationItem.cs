using System.Text.Json.Serialization;

namespace Param_RootNamespace.Models;

public class NavigationItem
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("icon")]
    public string? Icon { get; set; }

    [JsonPropertyName("navigateTo")]
    public string? NavigateTo { get; set; }

    [JsonPropertyName("children")]
    public List<NavigationItem>? Children { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = "item"; // item, separator, header
}
