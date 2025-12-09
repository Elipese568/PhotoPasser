using System;
using System.Threading.Tasks;

namespace PhotoPasser.Service;

public class InMemoryWorkspaceViewManager : IWorkspaceViewManager
{
    private WorkspaceViewState? _state;
    public event EventHandler<WorkspaceViewState>? StateChanged;

    public void Dispose()
    {
        // mock: no-op
    }

    public Task<WorkspaceViewState?> LoadAsync()
    {
        return Task.FromResult(_state ?? new WorkspaceViewState());
    }

    public Task SaveAsync(WorkspaceViewState state)
    {
        _state = state;
        StateChanged?.Invoke(this, state);
        return Task.CompletedTask;
    }
}
