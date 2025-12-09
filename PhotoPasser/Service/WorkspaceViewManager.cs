
using PhotoPasser.Helper;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Storage;

namespace PhotoPasser.Service;

public class WorkspaceViewManager : IWorkspaceViewManager
{
    private readonly StorageFolder _folder;
    private readonly string _fileName;
    private readonly object _sync = new();
    private WorkspaceViewState? _cached;

    public event EventHandler<WorkspaceViewState>? StateChanged;

    public WorkspaceViewManager(StorageFolder folder, string fileName = "workspace.json")
    {
        _folder = folder ?? throw new ArgumentNullException(nameof(folder));
        _fileName = fileName;
    }

    public async Task<WorkspaceViewState?> LoadAsync()
    {
        try
        {
            var file = await _folder.CreateFileAsync(_fileName, CreationCollisionOption.OpenIfExists);
            using var stream = await file.OpenStreamForReadAsync();
            if (stream.Length == 0)
            {
                _cached = new WorkspaceViewState();
            }
            else
            {
                _cached = JsonSerializer.Deserialize<WorkspaceViewState>(stream) ?? new WorkspaceViewState();
            }
            return _cached;
        }
        catch
        {
            return new WorkspaceViewState();
        }
    }

    public async Task SaveAsync(WorkspaceViewState state)
    {
        if (state == null) return;
     
        try
        {
            var file = await _folder.CreateFileAsync(_fileName, CreationCollisionOption.ReplaceExisting);
            using var stream = await file.OpenStreamForWriteAsync();
            JsonSerializer.Serialize(stream, state);
            lock (_sync)
            {
                _cached = state;
            }
            StateChanged?.Invoke(this, state);
        }
        catch
        {
            // swallow IO errors - best effort
        }
    }

    public async void Dispose()
    {
        await SaveAsync(_cached ?? new WorkspaceViewState());
    }
}
