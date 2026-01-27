using CommunityToolkit.Mvvm.ComponentModel;
using PhotoPasser.Models;
using PhotoPasser.Service;
using System.Threading.Tasks;
using Windows.System;
using System.Linq;
using System;

namespace PhotoPasser.ViewModels;

public partial class ResultViewModel : ObservableObject
{
    [ObservableProperty]
    private FiltResultViewModel currentResult;

    public ITaskDetailPhysicalManagerService TaskDpmService;
    public async Task GotoResultFolder()
    {
        var folder = await TaskDpmService.GetResultFolderAsync(CurrentResult.Model);
        await Launcher.LaunchFolderAsync(folder);
    }

    public async Task<string> GetResultFolder()
    {
        return (await TaskDpmService.GetResultFolderAsync(CurrentResult.Model)).Path;
    }
}
