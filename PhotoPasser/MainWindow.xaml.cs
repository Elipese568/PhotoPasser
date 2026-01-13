using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.WinUI;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using PhotoPasser.Primitive;
using PhotoPasser.Service.Primitive;
using PhotoPasser.Strings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
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
            this.AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
            InitializeComponent();

            languageChangedTip = new()
            {
                IconSource = new SymbolIconSource()
                {
                    Symbol = Symbol.Refresh
                },
                Target = LanguageSelectComboBox,
                XamlRoot = this.Content.XamlRoot,
            };
            (Content as Grid).Children.Add(languageChangedTip);

            _startLanguage = SettingProvider.Instance.Language;

            var presenter = this.AppWindow.Presenter as OverlappedPresenter;
            presenter.PreferredMinimumWidth = 1080 + 2;
            presenter.PreferredMinimumHeight = 512;
        }

        public Frame Frame => contentFrame;

        private void SettingButton_Click(object sender, RoutedEventArgs e)
        {
            ContentSplitPane.IsPaneOpen = true;
        }

        private bool _started = false;
        private TeachingTip languageChangedTip;
        private int _startLanguage;
        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(!_started || SettingProvider.Instance.Language == _startLanguage)
            {
                _started = true;
                return;
            }
            languageChangedTip.Title = $"LanguageChangedTipTitle_{SettingProvider.Instance.Language}".GetLocalized(LC.MainWindow);
            languageChangedTip.Subtitle = $"LanguageChangedTipDescription_{SettingProvider.Instance.Language}".GetLocalized(LC.MainWindow);
            DispatcherQueue.TryEnqueue(() =>
            {
                languageChangedTip.IsOpen = true;
            });
            
        }

        private string GetProjectGitCloneCommand() => ProjectProperties.ProjectGitCloneCommand;

        private void IssueCard_Click(object sender, RoutedEventArgs e)
        {
            Launcher.LaunchUriAsync(new (ProjectProperties.IssuesPageUrl));
        }

        private void StoreCard_Click(object sender, RoutedEventArgs e)
        {
            Launcher.LaunchUriAsync(new("https://apps.microsoft.com/detail/9PNKDZPF48DB"));
        }

        private void ShadowRect_Loaded(object sender, RoutedEventArgs e)
        {
            SettingPaneShadow.Receivers.Add(ShadowCastGrid);
        }
    }
}
