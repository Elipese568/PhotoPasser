using PhotoPasser.Helper;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage;

namespace PhotoPasser.Service.Mock;

public class MockTaskDetailPhysicalManagerService : ITaskDetailPhysicalManagerService
{
    private FiltTask _task;
    private TaskDetail _taskDetail = new TaskDetail();
    private StorageFolder _resultStorageFolder;
    private Dictionary<FiltResult,StorageFolder> _resultFolders = new();
    private readonly InMemoryWorkspaceViewManager _inMemoryManager = new InMemoryWorkspaceViewManager();

    // explicit interface implementation to satisfy interface contract
    IWorkspaceViewManager? ITaskDetailPhysicalManagerService.WorkspaceViewManager => _inMemoryManager;

    private static string[] _mockPicturePath = [
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
    public static string[] MockPicturePath => _mockPicturePath;
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
        var photosList = new List<PhotoInfo>();
        foreach (var p in _mockPicturePath)
        {
            var vm = await PhotoPasser.ViewModels.PhotoInfoViewModel.CreateAsync(p);
            photosList.Add(vm.Model);
        }

        var resultsList = new List<FiltResult>();
        for (int i = 1; i <= 3; i++)
        {
            var res = new FiltResult()
            {
                Name = "Test" + i.ToString(),
                Description = "This is a test result",
                Date = DateTime.Now,
                ResultId = Guid.NewGuid(),
                IsFavorite = false,
                Photos = new List<PhotoInfo>(),
                PinnedPhotos = new List<PhotoInfo>()
            };

            // pick photos for this result
            foreach (var p in _mockPicturePath)
            {
                if (Random.Shared.Next() % 2 == 0)
                {
                    var vm = await PhotoPasser.ViewModels.PhotoInfoViewModel.CreateAsync(p);
                    res.Photos.Add(vm.Model);
                }
                if (Random.Shared.Next() % 3 == 0)
                {
                    var vm2 = await PhotoPasser.ViewModels.PhotoInfoViewModel.CreateAsync(p);
                    res.PinnedPhotos.Add(vm2.Model);
                }
            }

            resultsList.Add(res);
        }

        _taskDetail = new TaskDetail()
        {
            Photos = photosList,
            Results = resultsList
        };
        return _taskDetail;
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
        return PhotoPasser.ViewModels.PhotoInfoViewModel.CreateAsync(fileUri.LocalPath).ContinueWith(t => t.Result.Model);
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