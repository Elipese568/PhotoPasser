using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoPasser.Helper;

public static class SystemThemeHelper
{
    private const string PersonalizeKeyPath =
        @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";

    /// <summary>
    /// 当前应用是否应使用深色模式（AppsUseLightTheme）
    /// </summary>
    public static bool IsAppDarkMode()
    {
        return ReadThemeValue("AppsUseLightTheme") == 0;
    }

    /// <summary>
    /// 系统 UI 是否为深色模式（任务栏、开始菜单）
    /// </summary>
    public static bool IsSystemDarkMode()
    {
        return ReadThemeValue("SystemUsesLightTheme") == 0;
    }

    private static int ReadThemeValue(string valueName)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(PersonalizeKeyPath);
            if (key?.GetValue(valueName) is int value)
                return value;
        }
        catch
        {
            // 忽略异常，走默认
        }

        // Windows 默认是浅色
        return 1;
    }
}
