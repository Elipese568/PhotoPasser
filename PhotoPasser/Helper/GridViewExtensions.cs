using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;
using System.Linq;

namespace PhotoPasser.Helper
{
    public static class GridViewExtensions
    {
        // Select all items into SelectedItems safely
        public static void SelectAllSafe(this GridView gridView)
        {
            if (gridView == null) return;
            gridView.SelectedItems.Clear();
            foreach (var item in gridView.Items)
            {
                gridView.SelectedItems.Add(item);
            }
        }

        // Return selected items as a typed list
        public static List<T> GetSelectedItemsAs<T>(this GridView gridView)
        {
            if (gridView == null) return new List<T>();
            return gridView.SelectedItems.Cast<T>().ToList();
        }

        // Count of selected items
        public static int SelectedCount(this GridView gridView)
        {
            return gridView?.SelectedItems?.Count ?? 0;
        }
    }
}
