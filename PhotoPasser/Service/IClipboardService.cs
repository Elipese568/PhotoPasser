using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;

namespace PhotoPasser.Service;

public interface IClipboardService
{
    Task SetStorageItemsAsync(IEnumerable<StorageFile> files);
    Task SetTextAsync(string text);
    Task SetBitmapAsync(RandomAccessStreamReference bitmapRef);
    Task<IReadOnlyList<StorageFile>> GetStorageFilesAsync();
}
