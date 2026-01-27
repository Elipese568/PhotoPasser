using System;
using CommunityToolkit.Mvvm.ComponentModel;
using PhotoPasser.Models;

namespace PhotoPasser.ViewModels;

public partial class FiltTaskViewModel : ObservableObject
{
    public FiltTask Model { get; }

    public Guid Id => Model.Id;
    public string Name
    {
        get => Model.Name;
        set
        {
            Model.Name = value;
            OnPropertyChanged(nameof(Name));
        }
    }
    public string Description
    {
        get => Model.Description;
        set
        {
            Model.Description = value;
            OnPropertyChanged(nameof(Description));
        }
    }
    public bool CopySource
    {
        get => Model.CopySource;
        set
        {
            Model.CopySource = value;
            OnPropertyChanged(nameof(CopySource));
        }
    }
    public string DestinationPath
    {
        get => Model.DestinationPath;
        set
        {
            Model.DestinationPath = value;
            OnPropertyChanged(nameof(DestinationPath));
        }
    }

    public string PresentPhoto
    {
        get => Model.PresentPhoto;
        set => Model.PresentPhoto = value;
    }

    public FiltTaskViewModel(FiltTask model) => Model = model;
}
