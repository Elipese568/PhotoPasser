using PhotoPasser.Helper;
using PhotoPasser.Service.Primitive;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Storage;

namespace PhotoPasser.Service;

[JsonStorageFile(FileName = "detail.json")]
public class TaskDetailPhysicalManagerService : ITaskDetailPhysicalManagerService
{
    private static readonly StorageFolder DetailStorageFolder = ApplicationData.Current.LocalFolder.CreateFolderAsync("Tasks", CreationCollisionOption.OpenIfExists).Sync();

    private StorageFolder _taskFileStorageFolder;
    private StorageFile _detailStorageFile;
    private TaskDetail _detail;

    private StorageFolder _srcCopyDestinationFolder;

    public async Task<Uri> CopySourceAsync(Uri source)
    {
        var sourceFile = await StorageItemProvider.GetStorageFile(source);
        var destFile = await sourceFile.CopyAsync(_srcCopyDestinationFolder, sourceFile.Name, NameCollisionOption.GenerateUniqueName);
        return new Uri(destFile.Path);
    }

    public async Task<TaskDetail> InitializeAsync(FiltTask task)
    {
        _taskFileStorageFolder = await DetailStorageFolder.CreateFolderAsync(task.Id.ToString(), CreationCollisionOption.OpenIfExists);

        _detailStorageFile = await _taskFileStorageFolder.CreateFileAsync("detail.json", CreationCollisionOption.OpenIfExists);
        _srcCopyDestinationFolder = await _taskFileStorageFolder.CreateFolderAsync("Sources", CreationCollisionOption.OpenIfExists);

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

    public void Dispose()
    {
        if (_detailStorageFile == null)
            return;
        _detailStorageFile.DeleteAsync().Sync();
        _detailStorageFile = _taskFileStorageFolder.CreateFileAsync("detail.json", CreationCollisionOption.OpenIfExists).Sync();
        using var serializedStream = _detailStorageFile.OpenStreamForWriteAsync().Sync();
        try
        {
            JsonSerializer.Serialize(serializedStream, _detail);
        }
        catch
        {
        }
    }
}

public class MockTaskDetailPhysicalManagerService : ITaskDetailPhysicalManagerService
{
    private FiltTask _task;
    private TaskDetail _taskDetail = new TaskDetail();

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

    public void Dispose()
    {
        return;
    }

    public async Task<TaskDetail> InitializeAsync(FiltTask task)
    {
        _task = task;
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
                    ResultId = Guid.NewGuid()
                }).EvalResults().AsObservableAsync()
        };
    }
}