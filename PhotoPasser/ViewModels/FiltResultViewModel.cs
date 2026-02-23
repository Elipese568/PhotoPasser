using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using PhotoPasser.Models;

namespace PhotoPasser.ViewModels;

public partial class FiltResultViewModel : ObservableObject
{
    public FiltResult Model { get; }

    public Guid ResultId => Model.ResultId;
    public string Name
    {
        get => Model.Name;
        set
        {
            Model.Name = value;
            OnPropertyChanged();
        }
    }
    public string Description
    {
        get => Model.Description;
        set
        {
            Model.Description = value; 
            OnPropertyChanged();
        }
    }

    public DateTime Date => Model.Date;

    public ObservableCollection<PhotoInfoViewModel> Photos { get; private set; }
    public ObservableCollection<PhotoInfoViewModel> PinnedPhotos { get; private set; }
    public bool IsFavorite
    {
        get => Model.IsFavorite;
        set
        {
            Model.IsFavorite = value;
            OnPropertyChanged();
        }
    }

    public FiltResultViewModel(FiltResult model)
    {
        Model = model;
        Photos = new ObservableCollection<PhotoInfoViewModel>((model.Photos ?? new List<PhotoInfo>()).Select(p => new PhotoInfoViewModel(p)));
        PinnedPhotos = new ObservableCollection<PhotoInfoViewModel>((model.PinnedPhotos ?? new List<PhotoInfo>()).Select(p => new PhotoInfoViewModel(p)));
    }

    public void SyncToModel()
    {
        Model.Photos = Photos.Select(vm => vm.Model).ToList();
        Model.PinnedPhotos = PinnedPhotos.Select(vm => vm.Model).ToList();
    }

    public override int GetHashCode()
    {
        return ResultId.GetHashCode();
    }
    public override bool Equals(object? obj)
    {
        if(obj is FiltResultViewModel other)
        {
            return this.ResultId == other.ResultId;
        }
        return false;
    }
}
