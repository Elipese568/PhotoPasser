using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.ViewManagement;

namespace PhotoPasser.Primitive;

public enum ApplicationThemeEx
{
    Light,
    Dark,
    System
}
public class SettingProvider : ObservableObject
{
    public static SettingProvider Instance
    {
        get
        {
            field ??= new SettingProvider();
            return field;
        }
    }
    public static void PreApplySetting()
    {
        Microsoft.Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride = Instance.Language switch
        {
            0 => "zh-CN",
            1 => "en-US",
            _ => "zh-CN"
        };
        Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride = Instance.Language switch
        {
            0 => "zh-CN",
            1 => "en-US",
            _ => "zh-CN"
        };
    }
    public static void ApplySetting()
    {
        (App.GetService<MainWindow>()!.Content as Grid).RequestedTheme = Instance.RequestedTheme switch
        {
            0 => ElementTheme.Default,
            1 => ElementTheme.Light,
            2 => ElementTheme.Dark,
            _ => ElementTheme.Default
		};
    }
    private EventHandlerWrapper<EventHandler> _themeChanged = EventHandlerWrapper<EventHandler>.Create();
	public event EventHandler ThemeChanged
    {
        add => _themeChanged.AddHandler(value);
        remove => _themeChanged.RemoveHandler(value);
	}
    private T GetSetting<T>(string key, T defaultValue = default)
    {
        var values = ApplicationData.GetDefault().LocalSettings.Values;
        if (values.ContainsKey(key))
            return (T)ApplicationData.GetDefault().LocalSettings.Values[key];
        else
        {
            ApplicationData.GetDefault().LocalSettings.Values.Add(key, defaultValue);
            return defaultValue;
        }
    }
    private void SetSetting<T>(string key, T value) => ApplicationData.GetDefault().LocalSettings.Values[key] = value;
    public int RequestedTheme
    {
        get => GetSetting<int>(nameof(RequestedTheme));
        set
        {
            SetSetting(nameof(RequestedTheme), value);
            App.GetService<MainWindow>()!.DispatcherQueue.TryEnqueue(() =>
            {
                (App.GetService<MainWindow>()!.Content as Grid).RequestedTheme = value switch
                {
                    0 => ElementTheme.Default,
                    1 => ElementTheme.Light,
                    2 => ElementTheme.Dark,
                    _ => ElementTheme.Default
                };
				_themeChanged.Invoke(this, EventArgs.Empty);
			});
            
			OnPropertyChanged();
        }
    }

    public int Language
    {
        get => GetSetting<int>(nameof(Language), 0);
        set
        {
            SetSetting(nameof(Language), value);
            OnPropertyChanged();
        }
    }

    public bool AdvancedImageView
    {
        get => GetSetting<bool>(nameof(AdvancedImageView));
        set
        {
            SetSetting<bool>(nameof(AdvancedImageView), value);
            OnPropertyChanged();
        }
    }
}
