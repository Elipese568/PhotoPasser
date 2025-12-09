using System;
using System.Threading.Tasks;

namespace PhotoPasser.Service;

public interface IWorkspaceViewManager : IDisposable
{
    Task<WorkspaceViewState?> LoadAsync();
    Task SaveAsync(WorkspaceViewState state);
    event EventHandler<WorkspaceViewState>? StateChanged;
}
