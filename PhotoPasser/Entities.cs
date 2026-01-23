using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Media;
using PhotoPasser.Helper;
using PhotoPasser.Primitive;
using Windows.UI.ViewManagement;

namespace PhotoPasser;

public partial class FiltTask : ObservableObject
{
    public FiltTask()
    {
    }

    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private string _description;

    public bool CopySource { get; set; }

    [ObservableProperty]
    private string _destinationPath;

    [ObservableProperty]
    private Guid _id;

    [ObservableProperty]
    private string _presentPhoto;
}

public partial class FiltResult : ObservableObject
{
    [ObservableProperty]
    private string _name;
    [ObservableProperty]
    private string _description;
    [ObservableProperty]
    private ObservableCollection<PhotoInfo> _photos;
    [ObservableProperty]
    private ObservableCollection<PhotoInfo> _pinnedPhotos;
    [ObservableProperty]
    private DateTime _date;
    [ObservableProperty]
    private bool _isFavorite;

    public Guid ResultId { get; set; }

    
}

public partial class TaskDetail : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<FiltResult> _results = new();
    [ObservableProperty]
    private ObservableCollection<PhotoInfo> _photos = new();
    [ObservableProperty]
    private ObservableCollection<PhotoInfo> _pinnedPhotos = new();
}

[JsonConverter(typeof(PhotoInfoJsonConverter))]
public partial class PhotoInfo : ObservableObject
{
    [ObservableProperty]
    private string _path;
    [ObservableProperty]
    private string _userName;
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public DateTime DateCreated { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public DateTime DateModified { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]

    public long Size { get; set; }

    public static async Task<PhotoInfo> Create(string filePath)
    {
        var info = await StorageItemProvider.GetFileInfo(filePath);
        return new()
        {
            Path = filePath,
            UserName = System.IO.Path.GetFileNameWithoutExtension(filePath),
            Size = info.Length,
            DateCreated = info.CreationTime,
            DateModified = info.LastWriteTime
        };
    }
}