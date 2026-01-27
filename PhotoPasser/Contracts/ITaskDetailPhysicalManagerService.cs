using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace PhotoPasser.Service;

public interface ITaskDetailPhysicalManagerService : IDisposable
{
    Task<TaskDetail> InitializeAsync(FiltTask task);

    Task<Uri> CopySourceAsync(Uri source);

    // Prepare a source path: validate that it is a loadable image and, if configured, copy it to managed storage.
    // Returns the Uri to the (possibly copied) file or null if the path is not a valid image.
    Task<Uri?> PrepareSourceAsync(string path);

    // Create a PhotoInfo instance from a file Uri (service may enrich metadata if needed).
    Task<PhotoInfo> CreatePhotoInfoAsync(Uri fileUri);

    // Delete a physical file if it exists. Implementations decide how to treat managed files.
    Task DeletePhotoFileIfExistsAsync(string path);

    // Persist any in-memory state (e.g. TaskDetail) to storage.
    Task SaveAsync();

    Task<StorageFolder> GetResultFolderAsync(FiltResult result);

    Task ProcessPhysicalResultAsync(FiltResult result);

    // Workspace view state manager for this task (may be null until InitializeAsync is called)
    IWorkspaceViewManager? WorkspaceViewManager { get; }
}
