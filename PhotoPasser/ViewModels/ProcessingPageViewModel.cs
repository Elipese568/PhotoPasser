using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using PhotoPasser.Models;
using PhotoPasser.ViewModels;
using PhotoPasser.Views;

namespace PhotoPasser.ViewModels;

public partial class ProcessingPageViewModel : ObservableObject
{
    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private string _description;

    [ObservableProperty]
    private string _step = "Setup";

    [ObservableProperty]
    private int _selectedPhotoIndex;

    [ObservableProperty]
    private int _acceptedCount;

    [ObservableProperty]
    private int _rejectedCount;

    public ObservableCollection<PhotoInfoViewModel> DisplayedPhotos { get; set; }
    public ObservableCollection<FiltingPhotoInfoViewModel> FiltingPhotos { get; set; }
}
