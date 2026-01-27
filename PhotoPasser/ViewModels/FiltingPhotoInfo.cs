using CommunityToolkit.Mvvm.ComponentModel;
using PhotoPasser.Models;
using PhotoPasser.Primitive;

namespace PhotoPasser.ViewModels;

public partial class FiltingPhotoInfoViewModel : ObservableObject
{
    public PhotoInfo Model { get; }

    [ObservableProperty]
    private FiltingStatus _status;

    public FiltingPhotoInfoViewModel(PhotoInfo model)
    {
        Model = model;
    }

    public static FiltingPhotoInfoViewModel Create(PhotoInfo info)
    {
        var vm = new FiltingPhotoInfoViewModel(info)
        {
            Status = FiltingStatus.Unset
        };
        return vm;
    }

    // pass-through properties for bindings
    public string Path => Model.Path;
    public string UserName
    {
        get => Model.UserName;
        set
        {
            Model.UserName = value;
            OnPropertyChanged(nameof(UserName));
        }
    }
}
