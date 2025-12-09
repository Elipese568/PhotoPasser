using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using PhotoPasser.Service.Primitive;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.ViewManagement;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace PhotoPasser
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(TitlebarArea);
            InitializeComponent();

            var presenter = this.AppWindow.Presenter as OverlappedPresenter;
            presenter.PreferredMinimumWidth = 1080 + 2;
            presenter.PreferredMinimumHeight = 512;
        }

        public Frame Frame => contentFrame;
    }
}
