using PhotoPasser.Helper;
using PhotoPasser.Service.Primitive;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace PhotoPasser.Service;

[JsonStorageFile(FileName = "detail.json")]
public class TaskDetailPhysicalManagerService : ITaskDetailPhysicalManagerService
{
    private static readonly StorageFolder DetailStorageFolder = ApplicationData.Current.LocalFolder.CreateFolderAsync("Tasks", CreationCollisionOption.OpenIfExists).Sync();
    private static readonly string[] _imageExts = [".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tif", ".tiff", ".webp", ".svg", ".heif", ".heic", ".ico", ".raw", ".cr2", ".nef", ".orf", ".arw"];
    private StorageFolder _taskFileStorageFolder;
    private StorageFile _detailStorageFile;
    private TaskDetail _detail;

    private StorageFolder _srcCopyDestinationFolder;
    private StorageFolder _destinationFolder;

    private FiltTask _task;

    // workspace manager instance
    private WorkspaceViewManager? _workspaceViewManager;

    public IWorkspaceViewManager? WorkspaceViewManager => _workspaceViewManager;

    public async Task<Uri> CopySourceAsync(Uri source)
    {
        var sourceFile = await StorageItemProvider.GetStorageFile(source);
        var destFile = await sourceFile.CopyAsync(_srcCopyDestinationFolder, sourceFile.Name, NameCollisionOption.GenerateUniqueName);
        return new Uri(destFile.Path);
    }

    public async Task<TaskDetail> InitializeAsync(FiltTask task)
    {
        _task = task; // keep reference for PrepareSource behavior
        _taskFileStorageFolder = await DetailStorageFolder.CreateFolderAsync(task.Id.ToString(), CreationCollisionOption.OpenIfExists);

        _detailStorageFile = await _taskFileStorageFolder.CreateFileAsync("detail.json", CreationCollisionOption.OpenIfExists);
        _srcCopyDestinationFolder = await _taskFileStorageFolder.CreateFolderAsync("Sources", CreationCollisionOption.OpenIfExists);
        _destinationFolder = await StorageFolder.GetFolderFromPathAsync(task.DestinationPath);

        // create workspace manager for task folder
        _workspaceViewManager = new WorkspaceViewManager(_taskFileStorageFolder);

        using var readStream = await _detailStorageFile.OpenStreamForReadAsync();
        if (readStream.Length == 0)
        {
            _detail = new TaskDetail();
        }
        else
        {
            try
            {
                _detail = JsonSerializer.Deserialize<TaskDetail>(readStream);
            }
            catch
            {
                _detail = new TaskDetail();
            }
        }

        return _detail;
    }

    public async Task<Uri?> PrepareSourceAsync(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        Uri fileUri;
        try
        {
            fileUri = new Uri(path);
        }
        catch
        {
            return null;
        }

        try
        {
            // validate image readability
            var locPath = fileUri.LocalPath;
            if (!_imageExts.Contains(Path.GetExtension(locPath).ToLower()))
                return null;
        }
        catch
        {
            return null;
        }

        if (_task != null && _task.CopySource)
        {
            return await CopySourceAsync(fileUri);
        }

        return fileUri;
    }

    public async Task<PhotoInfo> CreatePhotoInfoAsync(Uri fileUri)
    {
        if (fileUri == null) throw new ArgumentNullException(nameof(fileUri));
        // create enriched PhotoInfoViewModel and return its Model
        var vm = await PhotoPasser.ViewModels.PhotoInfoViewModel.CreateAsync(fileUri.LocalPath);
        return vm.Model;
    }

    public async Task DeletePhotoFileIfExistsAsync(string path)
    {
        try
        {
            var file = await StorageItemProvider.GetStorageFile(path, false);
            if (file != null)
            {
                await file.DeleteAsync();
            }
        }
        catch
        {
            // swallow - deletion best-effort
        }
    }

    public async Task SaveAsync()
    {
        if (_detailStorageFile == null)
            return;

        await _detailStorageFile.DeleteAsync();

        // replace content atomically by creating/truncating and writing
        try
        {
            _detailStorageFile = await _taskFileStorageFolder.CreateFileAsync("detail.json", CreationCollisionOption.ReplaceExisting);
            using var serializedStream = await _detailStorageFile.OpenStreamForWriteAsync();
            JsonSerializer.Serialize(serializedStream, _detail);
        }
        catch
        {
            // ignore persistence failures
        }
    }

    public async void Dispose()
    {
        // Persist state synchronously
        await SaveAsync();
        _workspaceViewManager.Dispose();
    }
    
    Dictionary<FiltResult, StorageFolder> _cachedResultFolders = new();

    public async Task<StorageFolder> GetResultFolderAsync(FiltResult result)
    {
        var folderName = $"{result.Name}_{result.ResultId.GetHashCode()}";
        StorageFolder folder = default;
        try
        {
            folder = await _destinationFolder.GetFolderAsync(folderName);
        }
        catch
        {
            SpinWait.SpinUntil(() => _cachedResultFolders.TryGetValue(result, out folder));
        }
        return folder;
    }

    public async Task ProcessPhysicalResultAsync(FiltResult result)
    {
        var folderName = $"{result.Name}_{result.ResultId.GetHashCode()}";
        StorageFolder folder;
        try
        {
            folder = await _destinationFolder.GetFolderAsync(folderName);
        }
        catch
        {
            folder = await _destinationFolder.CreateFolderAsync(folderName);
            foreach (var photo in result.Photos)
            {
                var file = await StorageItemProvider.GetStorageFile(photo.Path);
                var copiedFile = await file.CopyAsync(folder);
                if (photo.UserName + file.FileType != copiedFile.Name)
                    await copiedFile.RenameAsync(photo.UserName + file.FileType);
            }
        }
        _cachedResultFolders[result] = folder;
    }
}
