// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Xml.Linq;
using Microsoft.Templates.Core.Gen;
using Microsoft.Templates.UI.Mvvm;

namespace Microsoft.Templates.UI.ViewModels.NewItem
{
    public class ParentNavigationSelectionViewModel : Observable
    {
        private NavigationItemViewModel _selectedParent;
        private bool _addAsRootItem = true;
        private Visibility _noItemsMessageVisibility = Visibility.Collapsed;

        // Flat list of all navigation items for selection (with indentation info)
        public ObservableCollection<NavigationItemViewModel> AllNavigationItems { get; } = new ObservableCollection<NavigationItemViewModel>();

        public NavigationItemViewModel SelectedParent
        {
            get => _selectedParent;
            set
            {
                SetProperty(ref _selectedParent, value);
                if (value != null)
                {
                    AddAsRootItem = false;
                }
            }
        }

        public bool AddAsRootItem
        {
            get => _addAsRootItem;
            set
            {
                SetProperty(ref _addAsRootItem, value);
                if (value)
                {
                    _selectedParent = null;
                    OnPropertyChanged(nameof(SelectedParent));
                }
            }
        }

        public Visibility NoItemsMessageVisibility
        {
            get => _noItemsMessageVisibility;
            set => SetProperty(ref _noItemsMessageVisibility, value);
        }

        public void LoadNavigationItems()
        {
            // Reset selection state
            _addAsRootItem = true;
            _selectedParent = null;
            OnPropertyChanged(nameof(AddAsRootItem));
            OnPropertyChanged(nameof(SelectedParent));

            AllNavigationItems.Clear();

            var projectPath = GenContext.Current?.DestinationPath;
            System.Diagnostics.Debug.WriteLine($"[TabsNavView] LoadNavigationItems - ProjectPath: {projectPath}");

            if (string.IsNullOrEmpty(projectPath))
            {
                NoItemsMessageVisibility = Visibility.Visible;
                return;
            }

            var configPath = Path.Combine(projectPath, "Services", "NavigationConfig.xml");
            System.Diagnostics.Debug.WriteLine($"[TabsNavView] LoadNavigationItems - ConfigPath: {configPath}");

            if (!File.Exists(configPath))
            {
                System.Diagnostics.Debug.WriteLine($"[TabsNavView] LoadNavigationItems - Config file not found");
                NoItemsMessageVisibility = Visibility.Visible;
                return;
            }

            try
            {
                var xml = File.ReadAllText(configPath);
                System.Diagnostics.Debug.WriteLine($"[TabsNavView] LoadNavigationItems - XML content:\n{xml}");

                var doc = XDocument.Parse(xml);
                var rootItems = doc.Root?.Element("NavigationItems")?.Elements("NavigationItem");

                if (rootItems == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[TabsNavView] LoadNavigationItems - No NavigationItems element found");
                    NoItemsMessageVisibility = Visibility.Visible;
                    return;
                }

                // Recursively load all items with their depth level
                foreach (var element in rootItems)
                {
                    LoadNavigationItemRecursive(element, 0);
                }

                System.Diagnostics.Debug.WriteLine($"[TabsNavView] LoadNavigationItems - Total items: {AllNavigationItems.Count}");
                NoItemsMessageVisibility = AllNavigationItems.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TabsNavView] LoadNavigationItems - Error: {ex.Message}");
                NoItemsMessageVisibility = Visibility.Visible;
            }

            OnPropertyChanged(nameof(AllNavigationItems));
        }

        private void LoadNavigationItemRecursive(XElement element, int depth)
        {
            var type = element.Attribute("Type")?.Value ?? "item";
            if (type != "item")
            {
                return;
            }

            // Check nesting depth limit (max 3 levels of children)
            // depth 0 = root, depth 1 = first child, depth 2 = second child, depth 3 = third child (max)
            bool canHaveChildren = depth < 3;

            var item = new NavigationItemViewModel
            {
                Id = element.Attribute("Id")?.Value ?? string.Empty,
                Title = element.Attribute("Title")?.Value ?? string.Empty,
                Icon = element.Attribute("Icon")?.Value ?? string.Empty,
                Depth = depth,
                CanBeParent = canHaveChildren,
            };

            AllNavigationItems.Add(item);
            System.Diagnostics.Debug.WriteLine($"[TabsNavView] LoadNavigationItems - Added item: {item.Id} at depth {depth}, canBeParent={canHaveChildren}");

            // Load children recursively
            var children = element.Element("Children")?.Elements("NavigationItem");
            if (children != null)
            {
                foreach (var child in children)
                {
                    LoadNavigationItemRecursive(child, depth + 1);
                }
            }
        }

        public string GetSelectedParentId()
        {
            return AddAsRootItem ? null : SelectedParent?.Id;
        }
    }

    public class NavigationItemViewModel : Observable
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Icon { get; set; }
        public int Depth { get; set; }
        public bool CanBeParent { get; set; } = true;

        public string DisplayName => string.IsNullOrEmpty(Title) ? Id : Title;

        // Indentation for display (using spaces or a prefix)
        public string IndentedDisplayName => new string(' ', Depth * 4) + (Depth > 0 ? "└─ " : "") + DisplayName;

        // Left margin for visual hierarchy
        public Thickness ItemMargin => new Thickness(Depth * 20, 0, 0, 0);
    }
}
