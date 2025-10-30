using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;

namespace PhotoPasser.Helper;

public static class ShellInterop
{
    private const uint COINIT_APARTMENTTHREADED = 0x2;

    [DllImport("ole32.dll")]
    private static extern int CoInitializeEx(IntPtr pvReserved, uint dwCoInit);

    [DllImport("ole32.dll")]
    private static extern void CoUninitialize();

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHParseDisplayName(
        [MarshalAs(UnmanagedType.LPWStr)] string name,
        IntPtr pbc,
        out IntPtr ppidl,
        uint sfgaoIn,
        out uint psfgaoOut);

    [DllImport("shell32.dll")]
    private static extern void ILFree(IntPtr pidl);

    [DllImport("shell32.dll")]
    private static extern int SHCreateShellItemArrayFromIDLists(
        uint cidl,
        [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] IntPtr[] rgpidl,
        out IShellItemArray ppsiItemArray);

    // SHMultiFileProperties expects an IDataObject pointer
    [DllImport("shell32.dll")]
    private static extern int SHMultiFileProperties(
        [MarshalAs(UnmanagedType.Interface)] System.Runtime.InteropServices.ComTypes.IDataObject pdtobj,
        uint flags);

    // IShellItemArray COM interface (只声明我们会用到的方法)
    [ComImport, Guid("B63EA76D-1F85-456F-A19C-48159EFA858B"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellItemArray
    {
        // BindToHandler(IBindCtx *pbc, REFGUID bhid, REFIID riid, void **ppvOut);
        [PreserveSig]
        int BindToHandler(IntPtr pbc, [MarshalAs(UnmanagedType.LPStruct)] Guid bhid, [MarshalAs(UnmanagedType.LPStruct)] Guid riid, out IntPtr ppvOut);

        // 下面方法按声明顺序保留占位（未实现），但不需要在此示例中调用
        void GetPropertyStore(int flags, ref Guid riid, out IntPtr ppv);
        void GetPropertyDescriptionList(ref Guid keyType, ref Guid riid, out IntPtr ppv);
        void GetAttributes(uint dwAttribFlags, uint sfgaoMask, out uint psfgaoAttribs);
        void GetCount(out uint pdwNumItems);
        void GetItemAt(uint dwIndex, out IntPtr ppsi); // returns IShellItem*
        void EnumItems(out IntPtr ppenumShellItems);
    }

    // GUIDs
    private static readonly Guid BHID_DataObject = new Guid("b8c0bd9f-ed24-455c-83e6-d5390c4fe8c4");
    private static readonly Guid IID_IDataObject = new Guid("0000010e-0000-0000-C000-000000000046");
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)] 
    public struct SHELLEXECUTEINFO 
    { 
        public int cbSize; 
        public uint fMask; 
        public IntPtr hwnd; 
        [MarshalAs(UnmanagedType.LPWStr)] 
        public string lpVerb; 
        [MarshalAs(UnmanagedType.LPWStr)] 
        public string lpFile; 
        [MarshalAs(UnmanagedType.LPWStr)] 
        public string lpParameters; 
        [MarshalAs(UnmanagedType.LPWStr)] 
        public string lpDirectory; 
        public int nShow; 
        public IntPtr hInstApp; 
        public IntPtr lpIDList; 
        [MarshalAs(UnmanagedType.LPWStr)] 
        public string lpClass; 
        public IntPtr hkeyClass; 
        public uint dwHotKey; 
        public IntPtr hIcon; 
        public IntPtr hProcess; 
    }
    [DllImport("shell32.dll", CharSet = CharSet.Unicode)] 
    private static extern bool ShellExecuteEx(ref SHELLEXECUTEINFO lpExecInfo); 
    private const uint SEE_MASK_INVOKEIDLIST = 0x0000000C; 
    private const int SW_SHOW = 5; 
    public static bool ShowFileProperties(string filePath, IntPtr ownerHwnd) 
    { 
        var sei = new SHELLEXECUTEINFO 
        { 
            cbSize = Marshal.SizeOf<SHELLEXECUTEINFO>(), 
            fMask = SEE_MASK_INVOKEIDLIST, 
            hwnd = ownerHwnd, 
            lpVerb = "properties", 
            lpFile = filePath, 
            nShow = SW_SHOW 
        }; 
        return ShellExecuteEx(ref sei); 
    }
    /// <summary>
    /// 打开多个文件的属性窗口（支持跨目录，单一窗口显示“不同文件夹”）
    /// </summary>
    public static bool ShowFileProperties(string[] filePaths)
    {
        if (filePaths == null || filePaths.Length == 0) return false;

        // 保留真实存在的文件（排除文件夹）
        var validFiles = filePaths
            .Where(p => !string.IsNullOrWhiteSpace(p) && File.Exists(p))
            .Select(Path.GetFullPath)
            .ToArray();

        if (validFiles.Length == 0) return false;

        // 初始化 COM (STA)，记录是否需要 Uninitialize
        int hrInit = CoInitializeEx(IntPtr.Zero, COINIT_APARTMENTTHREADED);
        bool needUninit = hrInit == 0; // S_OK => we initialized; S_FALSE means already initialized

        var pidls = new List<IntPtr>();
        IntPtr pDataObjectRaw = IntPtr.Zero;
        System.Runtime.InteropServices.ComTypes.IDataObject? dataObject = null;

        try
        {
            // 解析每个文件为绝对 PIDL（PCIDLIST_ABSOLUTE）
            foreach (var path in validFiles)
            {
                if (SHParseDisplayName(path, IntPtr.Zero, out var pidl, 0, out _) == 0 && pidl != IntPtr.Zero)
                {
                    pidls.Add(pidl);
                }
            }

            if (pidls.Count == 0) return false;

            // 用 PIDL 数组创建 IShellItemArray
            var pidlArray = pidls.ToArray();
            if (SHCreateShellItemArrayFromIDLists((uint)pidlArray.Length, pidlArray, out var itemArray) != 0 || itemArray == null)
            {
                return false;
            }

            // 通过 IShellItemArray::BindToHandler 获取一个 IDataObject（BHID_DataObject）
            // 注意：BindToHandler 返回的是裸指针 (IUnknown*)，我们需要 Marshal.GetObjectForIUnknown
            int hr = itemArray.BindToHandler(IntPtr.Zero, BHID_DataObject, IID_IDataObject, out pDataObjectRaw);
            if (hr != 0 || pDataObjectRaw == IntPtr.Zero)
            {
                // 绑定失败 -> 可能系统不支持该绑定（极少数环境），返回 false
                return false;
            }

            // 把裸指针转换为 RCW (IDataObject)
            object? obj = Marshal.GetObjectForIUnknown(pDataObjectRaw);
            dataObject = obj as System.Runtime.InteropServices.ComTypes.IDataObject;

            if (dataObject == null)
            {
                // 无法转换为 IDataObject（异常情况），直接失败
                return false;
            }

            // 最后调用 SHMultiFileProperties
            int res = SHMultiFileProperties(dataObject, 0);
            return res == 0;
        }
        finally
        {
            // 释放裸指针（如果有）
            if (pDataObjectRaw != IntPtr.Zero)
            {
                Marshal.Release(pDataObjectRaw);
                pDataObjectRaw = IntPtr.Zero;
            }

            // 释放 pidl 列表
            foreach (var pidl in pidls)
            {
                if (pidl != IntPtr.Zero) ILFree(pidl);
            }

            if (needUninit)
            {
                CoUninitialize();
            }
        }
    }
}