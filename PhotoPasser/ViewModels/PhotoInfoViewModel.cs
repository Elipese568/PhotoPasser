using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using PhotoPasser.Models;
using PhotoPasser.Helper;

namespace PhotoPasser.ViewModels;

public partial class PhotoInfoViewModel : ObservableObject
{
    public PhotoInfo Model { get; }

    public string Path
    {
        get => Model.Path;
        set => Model.Path = value;
    }

    public string UserName
    {
        get => Model.UserName;
        set
        {
            Model.UserName = value;
            OnPropertyChanged(nameof(UserName));
        }
    }

    public long Size
    {
        get => Model.Size;
        set
        {
            Model.Size = value;
            OnPropertyChanged(nameof(Size));
        }
    }

    public DateTime DateCreated => Model.DateCreated;
    public DateTime DateModified => Model.DateModified;

    public PhotoInfoViewModel(PhotoInfo model)
    {
        Model = model;
    }

    public static async Task<PhotoInfoViewModel> CreateAsync(string filePath)
    {
        var info = await StorageItemProvider.GetFileInfo(filePath);
        var model = new PhotoInfo
        {
            Path = filePath,
            UserName = System.IO.Path.GetFileNameWithoutExtension(filePath),
            Size = info.Length
        };
        return Create(info, model);
    }

    private static PhotoInfoViewModel Create(System.IO.FileInfo info, PhotoInfo model)
    {
        return new PhotoInfoViewModel(model)
        {
        };
    }
}
