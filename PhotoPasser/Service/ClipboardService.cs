using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Streams;

namespace PhotoPasser.Service;

public class ClipboardService : IClipboardService
{
    public Task SetStorageItemsAsync(IEnumerable<StorageFile> files)
    {
        var dp = new DataPackage();
        dp.SetStorageItems(files.ToList());
        Clipboard.SetContent(dp);
        return Task.CompletedTask;
    }

    public Task SetTextAsync(string text)
    {
        var dp = new DataPackage();
        dp.SetText(text);
        Clipboard.SetContent(dp);
        return Task.CompletedTask;
    }

    public Task SetBitmapAsync(RandomAccessStreamReference bitmapRef)
    {
        var dp = new DataPackage();
        dp.SetBitmap(bitmapRef);
        Clipboard.SetContent(dp);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<StorageFile>> GetStorageFilesAsync()
    {
        var data = Clipboard.GetContent();
        if (data.Contains(StandardDataFormats.StorageItems))
        {
            var items = await data.GetStorageItemsAsync();
            return items.OfType<StorageFile>().ToList();
        }
        return new List<StorageFile>();
    }
}
