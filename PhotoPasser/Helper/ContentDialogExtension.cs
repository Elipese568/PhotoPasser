using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoPasser.Helper;

public static class ContentDialogExtension
{
    extension(ContentDialog cd)
    {
        public void ApplyApplicationOption()
        {
            cd.XamlRoot = App.GetService<MainWindow>()!.Content.XamlRoot;
            cd.RequestedTheme = (App.GetService<MainWindow>()!.Content as FrameworkElement).RequestedTheme;
            cd.Style = App.Current.Resources["DefaultContentDialogStyle"] as Style;
        }
    }
}
