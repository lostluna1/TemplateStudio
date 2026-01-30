// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Templates.UI.Converters;
using Microsoft.Templates.UI.Styles;
using Microsoft.Templates.UI.ViewModels.NewItem;

namespace Microsoft.Templates.UI.Views.NewItem
{
    public partial class ParentNavigationSelectionPage : Page
    {
        public ParentNavigationSelectionPage()
        {
            Resources.MergedDictionaries.Add(AllStylesDictionary.GetMergeDictionary());
            Resources.Add("HasItemsVisibilityConverter", new CollectionHasItemsVisibilityConverter());
            Resources.Add("BoolToVisibilityConverter", new BoolToVisibilityConverter());

            DataContext = MainViewModel.Instance;
            InitializeComponent();

            // Navigation items are loaded when the step is selected (OnStepUpdated in MainViewModel)
        }
    }

    /// <summary>
    /// Converts a collection to Visibility based on whether it has items.
    /// </summary>
    public class CollectionHasItemsVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool hasItems = false;

            if (value is ICollection collection)
            {
                hasItems = collection.Count > 0;
            }
            else if (value is IEnumerable enumerable)
            {
                var enumerator = enumerable.GetEnumerator();
                hasItems = enumerator.MoveNext();
                (enumerator as IDisposable)?.Dispose();
            }

            // If parameter is provided, invert the logic
            if (parameter != null)
            {
                hasItems = !hasItems;
            }

            return hasItems ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
