using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace PhotoPasser.Models;

public class TaskDetail
{
    public List<FiltResult> Results { get; set; } = new();
    public List<PhotoInfo> Photos { get; set; } = new();
    public List<PhotoInfo> PinnedPhotos { get; set; } = new();
}
