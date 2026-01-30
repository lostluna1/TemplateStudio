// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Microsoft.Templates.Core.PostActions.Catalog
{
    /// <summary>
    /// Post action that moves a navigation item to be a child of another navigation item in NavigationConfig.xml.
    /// This is used after the normal merge post action adds the item as a root item.
    /// </summary>
    public class MoveNavigationItemToParentPostAction : PostAction<string>
    {
        private readonly string _navigationConfigPath;
        private readonly string _itemId;
        private readonly string _parentNavigationId;

        public MoveNavigationItemToParentPostAction(
            string relatedTemplate,
            string navigationConfigPath,
            string itemId,
            string parentNavigationId)
            : base(relatedTemplate, navigationConfigPath)
        {
            _navigationConfigPath = navigationConfigPath;
            _itemId = itemId;
            _parentNavigationId = parentNavigationId;
        }

        internal override void ExecuteInternal()
        {
            System.Diagnostics.Debug.WriteLine($"[TabsNavView] MoveNavigationItemToParentPostAction.ExecuteInternal()");
            System.Diagnostics.Debug.WriteLine($"[TabsNavView] Config path: {_navigationConfigPath}");
            System.Diagnostics.Debug.WriteLine($"[TabsNavView] Item ID: {_itemId}");
            System.Diagnostics.Debug.WriteLine($"[TabsNavView] Parent ID: {_parentNavigationId}");

            if (!File.Exists(_navigationConfigPath) || string.IsNullOrEmpty(_parentNavigationId))
            {
                System.Diagnostics.Debug.WriteLine($"[TabsNavView] Skipping - file doesn't exist or parentId is empty");
                return;
            }

            var xml = File.ReadAllText(_navigationConfigPath);
            System.Diagnostics.Debug.WriteLine($"[TabsNavView] Original XML:\n{xml}");

            // Remove merge markers first
            xml = Regex.Replace(xml, @"<!--\s*\^\^\s*-->\s*[\r\n]*", "", RegexOptions.Multiline);
            xml = Regex.Replace(xml, @"<!--\s*\{\[\{\s*-->\s*[\r\n]*", "", RegexOptions.Multiline);
            xml = Regex.Replace(xml, @"<!--\s*\}\]\}\s*-->\s*[\r\n]*", "", RegexOptions.Multiline);

            XDocument doc;
            try
            {
                doc = XDocument.Parse(xml);
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TabsNavView] Failed to parse XML: {ex.Message}");
                return;
            }

            var navigationItems = doc.Root?.Element("NavigationItems");
            if (navigationItems == null)
            {
                System.Diagnostics.Debug.WriteLine($"[TabsNavView] NavigationItems element not found");
                return;
            }

            // Find the item to move (it should be at the root level after merge)
            var itemToMove = navigationItems.Elements("NavigationItem")
                .FirstOrDefault(e => e.Attribute("Id")?.Value == _itemId);

            if (itemToMove == null)
            {
                System.Diagnostics.Debug.WriteLine($"[TabsNavView] Item to move not found: {_itemId}");
                System.Diagnostics.Debug.WriteLine($"[TabsNavView] Available items: {string.Join(", ", navigationItems.Elements("NavigationItem").Select(e => e.Attribute("Id")?.Value))}");
                return;
            }

            // Find the parent item
            var parentItem = FindNavigationItemById(navigationItems, _parentNavigationId);
            if (parentItem == null)
            {
                System.Diagnostics.Debug.WriteLine($"[TabsNavView] Parent item not found: {_parentNavigationId}");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"[TabsNavView] Found item to move and parent item");

            // Remove the item from its current location
            itemToMove.Remove();

            // Add to parent's Children element
            var childrenElement = parentItem.Element("Children");
            if (childrenElement == null)
            {
                childrenElement = new XElement("Children");
                parentItem.Add(childrenElement);
            }

            childrenElement.Add(itemToMove);

            // Save the updated XML preserving the original format and line endings
            // Detect original line ending style
            var originalContent = File.ReadAllText(_navigationConfigPath);
            var lineEnding = originalContent.Contains("\r\n") ? "\r\n" : "\n";

            // Use File.WriteAllText to preserve special characters like &#xE80F;
            var xmlString = doc.Declaration != null
                ? doc.Declaration.ToString() + lineEnding + doc.Root.ToString()
                : "<?xml version=\"1.0\" encoding=\"utf-8\"?>" + lineEnding + doc.Root.ToString();

            // Normalize all line endings to match original
            xmlString = Regex.Replace(xmlString, @"\r?\n", lineEnding);

            File.WriteAllText(_navigationConfigPath, xmlString, new UTF8Encoding(false));

            System.Diagnostics.Debug.WriteLine($"[TabsNavView] Successfully moved item to parent");
            System.Diagnostics.Debug.WriteLine($"[TabsNavView] New XML:\n{File.ReadAllText(_navigationConfigPath)}");
        }

        private XElement FindNavigationItemById(XElement container, string id)
        {
            foreach (var item in container.Elements("NavigationItem"))
            {
                if (item.Attribute("Id")?.Value == id)
                {
                    return item;
                }

                var children = item.Element("Children");
                if (children != null)
                {
                    var found = FindNavigationItemById(children, id);
                    if (found != null)
                    {
                        return found;
                    }
                }
            }

            return null;
        }
    }
}
