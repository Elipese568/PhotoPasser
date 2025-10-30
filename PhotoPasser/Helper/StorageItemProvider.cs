using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace PhotoPasser.Helper;

public static class StorageItemProvider
{
    private static Dictionary<string, FileInfo> _fileInfoCache = new();

    public static async Task<StorageFile> GetStorageFile(string filePath, bool applicationFileFallback = true) 
        => await GetStorageFile(new Uri(filePath), applicationFileFallback);

    public static async Task<StorageFile> GetStorageFile(Uri fileUri, bool applicationFileFallback = true)
    {
        if (fileUri.Scheme == "ms-appx" || fileUri.Scheme == "ms-appdata")
        {
            if (!applicationFileFallback)
                return null;

            return await StorageFile.GetFileFromApplicationUriAsync(fileUri);
        }
        else
        {
            return await StorageFile.GetFileFromPathAsync(fileUri.ToString());
        }
    }

    public static async Task<StorageFolder> GetStorageFolderFromFileParent(string filePath, bool applicationFileFallback = true) 
        => await GetStorageFolderFromFileParent(new Uri(filePath), applicationFileFallback);
    public static async Task<StorageFolder> GetStorageFolderFromFileParent(Uri fileUri, bool applicationFileFallback = true)
    {
        var file = await GetStorageFile(fileUri, applicationFileFallback);
        if (file == null)
            return null;

        return await file.GetParentAsync();
    }

    public static async Task<string?> GetRawFilePath(string filePath)
    {
        var file = await GetStorageFile(filePath);
        return file?.Path;
    }
    public static async Task<FileInfo> GetFileInfo(Uri fileUri, bool returnNullOnUnexist = false)
        => await GetFileInfo(fileUri.LocalPath, returnNullOnUnexist);
    public static async Task<FileInfo?> GetFileInfo(string filePath, bool returnNullOnUnexist = false)
    {
        var raw = await GetRawFilePath(filePath);
        if (_fileInfoCache.TryGetValue(raw, out FileInfo? value))
            return value;
        try
        {
            var fileInfo = new FileInfo(raw);
            if(fileInfo.Exists == false && returnNullOnUnexist)
                return null;
            _fileInfoCache.Add(raw, fileInfo);
            return fileInfo;
        }
        catch
        {
            return null;
        }
    }
}
