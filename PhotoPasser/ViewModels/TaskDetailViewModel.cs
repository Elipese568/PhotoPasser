using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace PhotoPasser.Models;

public class TaskDetailViewModel
{
    public TaskDetail Model { get; set; }
    public TaskDetailViewModel(TaskDetail detail)
    {
        Model = detail;
        Results = new(Model.Results);
        Photos = new(Model.Photos);
        PinnedPhotos = new(Model.PinnedPhotos);
    }

    public ObservableCollection<FiltResult> Results { get; set; }
    public ObservableCollection<PhotoInfo> Photos { get; set; }
    public ObservableCollection<PhotoInfo> PinnedPhotos { get; set; }
}
