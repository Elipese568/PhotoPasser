using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using PhotoPasser.Dialog;
using PhotoPasser.Helper;
using PhotoPasser.Service;
using PhotoPasser.Strings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace PhotoPasser.Views;

using PhotoPasser.ViewModels;



/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class TaskOverview : Page
{
    public TaskOverviewViewModel ViewModel { get; set; }

    public TaskOverview()
    {
        InitializeComponent();

        ViewModel = App.GetService<TaskOverviewViewModel>();
    }

    private void LibraryItem_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        VisualStateManager.GoToState(sender as Control, "OptionSettingButtonVisible", true);
    }

    private void LibraryItem_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        VisualStateManager.GoToState(sender as Control, "OptionSettingButtonDefault", true);
    }

    private void TaskItemPresenter_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        App.GetService<MainWindow>()!.Frame.Navigate(typeof(TaskWorkspace), (sender as FrameworkElement).DataContext, new Microsoft.UI.Xaml.Media.Animation.DrillInNavigationTransitionInfo());
    }

    private void Border_Loaded(object sender, RoutedEventArgs e)
    {

    }

    private void ShadowRect_Loaded(object sender, RoutedEventArgs e)
    {
        shadow.Receivers.Add(ShadowCastGrid);
    }
}
