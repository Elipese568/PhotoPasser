using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoPasser.Converters;

using Microsoft.UI.Xaml.Data;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

public class FileTypeFriendlyNameConverter : IValueConverter
{
    [DllImport("shell32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SHGetFileInfo(
        string pszPath,
        uint dwFileAttributes,
        out SHFILEINFO psfi,
        uint cbFileInfo,
        uint uFlags);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct SHFILEINFO
    {
        public IntPtr hIcon;
        public int iIcon;
        public uint dwAttributes;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szDisplayName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string szTypeName;
    }

    private const uint SHGFI_TYPENAME = 0x000000400;
    private const uint SHGFI_USEFILEATTRIBUTES = 0x000000010;
    private const uint FILE_ATTRIBUTE_NORMAL = 0x00000080;

    public static string GetFriendlyTypeName(string extension)
    {
        if (!extension.StartsWith("."))
            extension = "." + extension;

        SHFILEINFO shinfo;
        SHGetFileInfo(extension, FILE_ATTRIBUTE_NORMAL, out shinfo,
            (uint)Marshal.SizeOf(typeof(SHFILEINFO)),
            SHGFI_TYPENAME | SHGFI_USEFILEATTRIBUTES);

        return shinfo.szTypeName;
    }

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return GetFriendlyTypeName(Path.GetExtension(value as string));
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

