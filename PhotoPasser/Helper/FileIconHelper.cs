using System;
using System.Runtime.InteropServices;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using Windows.Storage.Streams;
using System.Runtime.InteropServices.WindowsRuntime;


namespace PhotoPasser.Helper;

public static class FileIconStreamHelper
{
    [DllImport("shell32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SHGetFileInfo(
        string pszPath,
        uint dwFileAttributes,
        out SHFILEINFO psfi,
        uint cbFileInfo,
        uint uFlags);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(IntPtr hIcon);

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

    private const uint SHGFI_ICON = 0x000000100;
    private const uint SHGFI_USEFILEATTRIBUTES = 0x000000010;
    private const uint SHGFI_LARGEICON = 0x000000000; // 大图标
    private const uint SHGFI_SMALLICON = 0x000000001; // 小图标
    private const uint FILE_ATTRIBUTE_NORMAL = 0x00000080;

    public static IRandomAccessStream GetFileTypeIconStream(string extension, bool largeIcon = true)
    {
        if (!extension.StartsWith("."))
            extension = "." + extension;

        SHFILEINFO shinfo;
        uint flags = SHGFI_ICON | SHGFI_USEFILEATTRIBUTES | (largeIcon ? SHGFI_LARGEICON : SHGFI_SMALLICON);

        SHGetFileInfo(extension, FILE_ATTRIBUTE_NORMAL, out shinfo,
            (uint)Marshal.SizeOf(typeof(SHFILEINFO)),
            flags);

        if (shinfo.hIcon == IntPtr.Zero)
            return null;

        try
        {
            using var icon = Icon.FromHandle(shinfo.hIcon);
            using var bmp = icon.ToBitmap();
            var ms = new MemoryStream();
            bmp.Save(ms, ImageFormat.Png);
            ms.Position = 0;
            return ms.AsRandomAccessStream();
        }
        finally
        {
            DestroyIcon(shinfo.hIcon); // 🧹 释放 HICON
        }
    }
}


