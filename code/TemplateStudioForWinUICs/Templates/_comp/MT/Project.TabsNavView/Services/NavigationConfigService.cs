using System.Xml.Linq;
using Param_RootNamespace.Contracts.Services;
using Param_RootNamespace.Models;

namespace Param_RootNamespace.Services;

public class NavigationConfigService : INavigationConfigService
{
    public async Task<List<NavigationItem>> LoadNavigationItemsAsync()
    {
        string xml;
        try
        {
            // Try packaged app path first
            var uri = new Uri("ms-appx:///Services/NavigationConfig.xml");
            var file = await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(uri);
            xml = await Windows.Storage.FileIO.ReadTextAsync(file);
        }
        catch
        {
            // Fall back to unpackaged app path
            var filePath = Path.Combine(AppContext.BaseDirectory, "Services", "NavigationConfig.xml");
            xml = await File.ReadAllTextAsync(filePath);
        }

        // Remove merge markers that template engine leaves
        xml = System.Text.RegularExpressions.Regex.Replace(xml, @"<!--\s*\^\^\s*-->\s*[\r\n]*", "", System.Text.RegularExpressions.RegexOptions.Multiline);
        xml = System.Text.RegularExpressions.Regex.Replace(xml, @"<!--\s*\{\[\{\s*-->\s*[\r\n]*", "", System.Text.RegularExpressions.RegexOptions.Multiline);
        xml = System.Text.RegularExpressions.Regex.Replace(xml, @"<!--\s*\}\]\}\s*-->\s*[\r\n]*", "", System.Text.RegularExpressions.RegexOptions.Multiline);

        var doc = XDocument.Parse(xml);
        var items = new List<NavigationItem>();

        foreach (var element in doc.Root?.Element("NavigationItems")?.Elements("NavigationItem") ?? Enumerable.Empty<XElement>())
        {
            var item = ParseNavigationItem(element);
            if (item != null)
            {
                items.Add(item);
            }
        }

        return items;
    }

    private NavigationItem? ParseNavigationItem(XElement element)
    {
        var item = new NavigationItem
        {
            Id = element.Attribute("Id")?.Value ?? string.Empty,
            Title = element.Attribute("Title")?.Value ?? string.Empty,
            Icon = element.Attribute("Icon")?.Value ?? string.Empty,
            NavigateTo = element.Attribute("NavigateTo")?.Value ?? string.Empty,
            Type = element.Attribute("Type")?.Value ?? "item"
        };

        // Parse children if any
        var childrenElement = element.Element("Children");
        if (childrenElement != null)
        {
            item.Children = new List<NavigationItem>();
            foreach (var childElement in childrenElement.Elements("NavigationItem"))
            {
                var child = ParseNavigationItem(childElement);
                if (child != null)
                {
                    item.Children.Add(child);
                }
            }
        }

        return item;
    }
}
