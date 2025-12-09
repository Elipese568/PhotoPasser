using PhotoPasser.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System;

namespace PhotoPasser.Helper;

public static class PhotoItemOperationUtils
{
    private static readonly IClipboardService _clipboardService = App.GetService<IClipboardService>();
    public static async Task<IList<StorageFile>> Copy(IList<PhotoInfo> photos)
    {
        var files = new List<StorageFile>();
        foreach (var photo in photos)
        {
            var file = await StorageItemProvider.GetStorageFile(photo.Path);
            if (file != null) files.Add(file);
        }
        return files;
    }
    public static async Task CopyAsBitmap(PhotoInfo photo)
    {
        await _clipboardService.SetBitmapAsync(RandomAccessStreamReference.CreateFromFile(await StorageItemProvider.GetStorageFile(photo.Path)));
    }
    public static async Task Rename(PhotoInfo photoInfo, string newName)
    {
        photoInfo.UserName = newName;
    }
    public static async Task CopyAsPath(IList<PhotoInfo> photos)
    {
        await _clipboardService.SetTextAsync(string.Join(Environment.NewLine, photos.Select(x => x.Path)));
    }
    
}
