// Portions of this file are derived from WinUIEssentials
// https://github.com/HO-COOH/WinUIEssentials
//
// Original work Copyright (c) HO-COOH
// Modifications and C# port Copyright (c) Elipese
//
// Licensed under the MIT License.

using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.ViewManagement;

namespace PhotoPasser.Primitive;

internal static class ThemeSettingsImpl
{
    private const string PersonalizeSubKey =
        @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";

    private const string DwmSubKey =
        @"Software\Microsoft\Windows\DWM";

    // ================= Registry helpers =================

    private static uint? TryGetDword(string subKey, string valueName)
    {
        using var key = Registry.CurrentUser.OpenSubKey(subKey);
        return key?.GetValue(valueName) as int? is int v ? (uint)v : null;
    }

    private static uint GetDword(string subKey, string valueName)
    {
        using var key = Registry.CurrentUser.OpenSubKey(subKey)
            ?? throw new InvalidOperationException("Registry key not found");

        return Convert.ToUInt32(key.GetValue(valueName));
    }

    // ================= Theme flags =================

    public static bool AppsUseLightTheme()
    {
        // 未激活 Windows 时该 key 可能不存在
        return TryGetDword(PersonalizeSubKey, "AppsUseLightTheme")
            .GetValueOrDefault(1) != 0;
    }

    public static bool ColorPrevalence()
    {
        return TryGetDword(PersonalizeSubKey, "ColorPrevalence")
            .GetValueOrDefault(0) != 0;
    }

    public static bool EnableTransparency()
    {
        return TryGetDword(PersonalizeSubKey, "EnableTransparency")
            .GetValueOrDefault(1) != 0;
    }

    public static bool SystemUsesLightTheme()
    {
        return TryGetDword(PersonalizeSubKey, "SystemUsesLightTheme")
            .GetValueOrDefault(1) != 0;
    }

    // ================= Accent color =================

    public static uint AccentColor()
    {
        uint colorization;
        bool opaque;

        DwmGetColorizationColor(out colorization, out opaque);
        return colorization;
    }

    public static uint ColorizationColor()
    {
        return GetDword(DwmSubKey, "ColorizationColor");
    }

    public static bool ShowAccentColorOnTitleBarsAndWindowBorders()
    {
        return ColorPrevalence();
    }

    // ================= Color history =================

    public sealed class ColorHistoryCollection
    {
        private const string SubKey =
            @"Software\Microsoft\Windows\CurrentVersion\Themes\History\Colors";

        public uint this[int index]
        {
            get
            {
                using var key = Registry.CurrentUser.OpenSubKey(SubKey)
                    ?? throw new InvalidOperationException();

                return Convert.ToUInt32(
                    key.GetValue($"ColorHistory{index}")
                );
            }
        }

        public int Count
        {
            get
            {
                using var key = Registry.CurrentUser.OpenSubKey(SubKey);
                return key?.GetValueNames().Length ?? 0;
            }
        }
    }

    public static ColorHistoryCollection ColorHistory { get; } = new();

    // ================= Color conversion =================

    public static Color ColorFromDWORDFromReg(uint value)
    {
        return Color.FromArgb(
            0xFF,
            (byte)(value & 0xFF),
            (byte)((value >> 8) & 0xFF),
            (byte)((value >> 16) & 0xFF)
        );
    }

    public static Color ColorFromDWORDFromDwm(uint value)
    {
        return Color.FromArgb(
            0xFF,
            (byte)((value >> 16) & 0xFF),
            (byte)((value >> 8) & 0xFF),
            (byte)(value & 0xFF)
        );
    }

    // ================= DWM interop =================

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmGetColorizationColor(
        out uint pcrColorization,
        out bool pfOpaqueBlend
    );
}


public sealed class ThemeSettings
{
    // ================= Singleton =================

    private static readonly Lazy<ThemeSettings> _instance =
        new(() => new ThemeSettings());

    public static ThemeSettings Instance => _instance.Value;

    private ThemeSettings() { }

    // ================= Flags =================

    public bool AppsUseLightTheme()
        => ThemeSettingsImpl.AppsUseLightTheme();

    public bool ColorPrevalence()
        => ThemeSettingsImpl.ColorPrevalence();

    public bool EnableTransparency()
        => ThemeSettingsImpl.EnableTransparency();

    public bool SystemUsesLightTheme()
        => ThemeSettingsImpl.SystemUsesLightTheme();

    // ================= Accent =================

    public Color AccentColor()
    {
        // 与你 C++ 中最终选择的路径一致
        var uiSettings = new UISettings();
        return uiSettings.GetColorValue(UIColorType.Accent);
    }

    public SolidColorBrush AccentColorBrush()
    {
        return new SolidColorBrush(AccentColor());
    }

    // ================= Colorization =================

    public Color ColorizationColor()
    {
        return ThemeSettingsImpl.ColorFromDWORDFromReg(
            ThemeSettingsImpl.ColorizationColor()
        );
    }

    public SolidColorBrush ColorizationColorBrush()
    {
        return new SolidColorBrush(ColorizationColor());
    }

    // ================= Color history =================

    public IList<object> ColorHistory()
    {
        var history = ThemeSettingsImpl.ColorHistory;
        var list = new List<object>(history.Count);

        for (int i = 0; i < history.Count; i++)
        {
            list.Add(
                ThemeSettingsImpl.ColorFromDWORDFromReg(history[i])
            );
        }

        return list;
    }

    public IList<object> ColorHistoryBrushes()
    {
        var history = ThemeSettingsImpl.ColorHistory;
        var list = new List<object>(history.Count);

        for (int i = 0; i < history.Count; i++)
        {
            list.Add(
                new SolidColorBrush(
                    ThemeSettingsImpl.ColorFromDWORDFromReg(history[i])
                )
            );
        }

        return list;
    }
}


public sealed class WindowCaptionButtonThemeWorkaround : DependencyObject
{
    private AppWindowTitleBar _titleBar;

    public WindowCaptionButtonThemeWorkaround()
    {
        ActualThemeChanged += (_, __) =>
        {
            ActualTheme = (Window.Content as FrameworkElement).ActualTheme;
            SetCaptionButtonTheme(ActualTheme);
        };
    }

    // === WinUI 中通常这是一个 DependencyObject / Control ===
    public event TypedEventHandler<FrameworkElement, object> ActualThemeChanged;

    public ElementTheme ActualTheme { get; private set; }

    public Window Window
    {
        get => field;
        set
        {
            field = value;
            _titleBar = field.AppWindow.TitleBar;
            ActualTheme = (field.Content as FrameworkElement).ActualTheme;
            (field.Content as FrameworkElement).ActualThemeChanged += (sender, args) =>
            {
                ActualThemeChanged(sender, args);
            };
            SetCaptionButtonTheme(ActualTheme);
        }
    }

    // ================== Color 运算替代 operator ==================

    private static Color Multiply(Color color, double value)
    {
        return Color.FromArgb(
            (byte)(color.A * value),
            (byte)(color.R * value),
            (byte)(color.G * value),
            (byte)(color.B * value)
        );
    }

    private static Color Add(Color lhs, Color rhs)
    {
        return Color.FromArgb(
            (byte)(lhs.A + rhs.A),
            (byte)(lhs.R + rhs.R),
            (byte)(lhs.G + rhs.G),
            (byte)(lhs.B + rhs.B)
        );
    }

    // Alpha 不生效，手动做 alpha blending
    private static Color GetPressedForeground(Color foreground, Color background)
    {
        return Add(
            Multiply(foreground, 0.5),
            Multiply(background, 0.5)
        );
    }

    private static bool IsColorLight(Color clr)
    {
        return ((5 * clr.G) + (2 * clr.R) + clr.B) > (8 * 128);
    }

    // ================== 核心逻辑 ==================

    private void SetCaptionButtonTheme(ElementTheme theme)
    {
        try
        {
            var foreground = theme == ElementTheme.Dark
                ? Colors.White
                : Colors.Black;

            _titleBar.ButtonForegroundColor = foreground;

            if (ThemeSettingsImpl.ShowAccentColorOnTitleBarsAndWindowBorders())
            {
                var hoverBackground = ThemeSettings.Instance.AccentColor();

                _titleBar.ButtonHoverBackgroundColor = hoverBackground;
                _titleBar.ButtonPressedBackgroundColor = hoverBackground;

                var hoverForeground = IsColorLight(hoverBackground)
                    ? Colors.Black
                    : Colors.White;

                _titleBar.ButtonHoverForegroundColor = hoverForeground;
                _titleBar.ButtonPressedForegroundColor =
                    GetPressedForeground(hoverForeground, hoverBackground);
            }
            else
            {
                /*
                    when ShowAccentColorOnTitleBarsAndWindowBorders is false,
                    the caption button colors are hard-coded:

                        light theme:
                            hover bg    #E9E9E9
                            pressed bg  #EDEDED
                            fg          black

                        dark theme:
                            hover bg    #2D2D2D
                            pressed bg  #292929
                            fg          white
                */

                var hoverBackground = theme == ElementTheme.Dark
                    ? Color.FromArgb(0xFF, 0x2D, 0x2D, 0x2D)
                    : Color.FromArgb(0xFF, 0xE9, 0xE9, 0xE9);

                var pressedBackground = theme == ElementTheme.Dark
                    ? Color.FromArgb(0xFF, 0x29, 0x29, 0x29)
                    : Color.FromArgb(0xFF, 0xED, 0xED, 0xED);

                var fg = theme == ElementTheme.Dark
                    ? Colors.White
                    : Colors.Black;

                _titleBar.ButtonHoverBackgroundColor = hoverBackground;
                _titleBar.ButtonHoverForegroundColor = fg;
                _titleBar.ButtonPressedBackgroundColor = pressedBackground;
                _titleBar.ButtonPressedForegroundColor =
                    GetPressedForeground(fg, pressedBackground);
            }
        }
        catch
        {
            // 防止窗口已经关闭但 TitleBar 仍被引用导致异常
        }
    }
}
