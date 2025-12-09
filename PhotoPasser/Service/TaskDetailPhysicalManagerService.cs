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
        return await PhotoInfo.Create(fileUri.LocalPath);
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

public class MockTaskDetailPhysicalManagerService : ITaskDetailPhysicalManagerService
{
    private FiltTask _task;
    private TaskDetail _taskDetail = new TaskDetail();
    private StorageFolder _resultStorageFolder;
    private Dictionary<FiltResult,StorageFolder> _resultFolders = new();
    private readonly InMemoryWorkspaceViewManager _inMemoryManager = new InMemoryWorkspaceViewManager();

    // explicit interface implementation to satisfy interface contract
    IWorkspaceViewManager? ITaskDetailPhysicalManagerService.WorkspaceViewManager => _inMemoryManager;

    private string[] _mockPicturePath = [
     "ms-appx:///Assets/Mock/20250407_133801.jpg",
     "ms-appx:///Assets/Mock/20250407_134009.jpg",
     "ms-appx:///Assets/Mock/20250407_134024.jpg",
     "ms-appx:///Assets/Mock/20250407_134045.jpg",
     "ms-appx:///Assets/Mock/20250407_134053.jpg",
     "ms-appx:///Assets/Mock/20250407_134117.jpg",
     "ms-appx:///Assets/Mock/20250407_134620.jpg",
     "ms-appx:///Assets/Mock/20250407_134738.jpg",
     "ms-appx:///Assets/Mock/20250407_134746.jpg",
     "ms-appx:///Assets/Mock/20250407_134754.jpg",
     "ms-appx:///Assets/Mock/20250407_230919.jpg",
     "ms-appx:///Assets/Mock/20250407_230924.jpg",
     "ms-appx:///Assets/Mock/20250407_230941.jpg",
     "ms-appx:///Assets/Mock/20250410_233640.jpg",
     "ms-appx:///Assets/Mock/20250410_233648.jpg",
     "ms-appx:///Assets/Mock/20250410_233727.jpg",
     "ms-appx:///Assets/Mock/20250410_234418.jpg",
     "ms-appx:///Assets/Mock/20250410_234449.jpg",
     "ms-appx:///Assets/Mock/20250410_234523.jpg",
     "ms-appx:///Assets/Mock/20250410_235129.jpg",
     "ms-appx:///Assets/Mock/20250412_121319.jpg",
     "ms-appx:///Assets/Mock/20250412_121327.jpg",
     "ms-appx:///Assets/Mock/20250412_121339.jpg",
     "ms-appx:///Assets/Mock/20250412_121357.jpg",
     "ms-appx:///Assets/Mock/20250405_204636.jpg",
     "ms-appx:///Assets/Mock/20250405_210815.jpg",
     "ms-appx:///Assets/Mock/20250405_234114.jpg",
     "ms-appx:///Assets/Mock/20250406_035043.jpg",
     "ms-appx:///Assets/Mock/20250406_035256.jpg",
     "ms-appx:///Assets/Mock/20250406_035309.jpg",
     "ms-appx:///Assets/Mock/20250406_035935.jpg",
     "ms-appx:///Assets/Mock/20250406_040857.jpg",
     "ms-appx:///Assets/Mock/20250406_040932.jpg",
     "ms-appx:///Assets/Mock/20250406_041312.jpg",
     "ms-appx:///Assets/Mock/20250406_192557.jpg",
     "ms-appx:///Assets/Mock/20250407_081654.jpg",
     "ms-appx:///Assets/Mock/20250407_081921.jpg",
     "ms-appx:///Assets/Mock/20250407_082111.jpg",
     "ms-appx:///Assets/Mock/20250407_101154.jpg",
     "ms-appx:///Assets/Mock/20250407_101157.jpg",
     "ms-appx:///Assets/Mock/20250407_101159.jpg"
    ];

    public Task<Uri> CopySourceAsync(Uri source)
    {
        return Task.FromResult(source);
    }

    public async void Dispose()
    {
        foreach (var f in _resultFolders)
            await f.Value.DeleteAsync();
        return;
    }

    public async Task<TaskDetail> InitializeAsync(FiltTask task)
    {
        _task = task;
        _resultStorageFolder = await StorageFolder.GetFolderFromPathAsync(task.DestinationPath);
        return _taskDetail = new TaskDetail()
        {
            Photos = await _mockPicturePath.Select(async x => await PhotoInfo.Create(x)).EvalResults().AsObservableAsync(),
            Results = await Enumerable.Range(1, 3).Select(async x =>
            new FiltResult()
            {
                Name = "Test" + x.ToString(),
                Description = "This is a test result",
                Date = DateTime.Now,
                Photos = await _mockPicturePath.Use(x => Random.Shared.Next() % 2 == 0).Select(async x => await PhotoInfo.Create(x)).EvalResults().AsObservableAsync(),
                ResultId = Guid.NewGuid(),
                PinnedPhotos = await _mockPicturePath.Use(x => Random.Shared.Next() % 3 == 0).Select(async x => await PhotoInfo.Create(x)).EvalResults().AsObservableAsync(),
                IsFavorite = false
            }).EvalResults().AsObservableAsync()
        };
    }

    public Task<Uri?> PrepareSourceAsync(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return Task.FromResult<Uri?>(null);
        try
        {
            var uri = new Uri(path);
            // basic validation by attempting to create PhotoInfo
            return Task.FromResult<Uri?>(uri);
        }
        catch
        {
            return Task.FromResult<Uri?>(null);
        }
    }

    public Task<PhotoInfo> CreatePhotoInfoAsync(Uri fileUri)
    {
        return PhotoInfo.Create(fileUri.LocalPath);
    }

    public Task DeletePhotoFileIfExistsAsync(string path)
    {
        // mock: no-op
        return Task.CompletedTask;
    }

    public Task SaveAsync()
    {
        // mock: no-op
        return Task.CompletedTask;
    }

    public async Task<StorageFolder> GetResultFolderAsync(FiltResult result)
    {
        if (_resultFolders.TryGetValue(result, out var folderResult))
            return folderResult;

        folderResult = _resultFolders[result] = await _resultStorageFolder.CreateFolderAsync(result.ResultId.ToString());

        foreach(var photo in result.Photos)
        {
            var file = await StorageItemProvider.GetStorageFile(photo.Path);
            var copiedFile = await file.CopyAsync(folderResult);
            await copiedFile.RenameAsync(photo.UserName + "." + file.FileType);
        }

        return folderResult;
    }

    public async Task ProcessPhysicalResultAsync(FiltResult result)
    {
        await Task.Delay(5000);
    }
}