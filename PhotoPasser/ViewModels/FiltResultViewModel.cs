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
        set => Model.Name = value;
    }
    public string Description
    {
        get => Model.Description;
        set => Model.Description = value;
    }

    public DateTime Date
    {
        get => Model.Date;
        set => Model.Date = value;
    }

    public ObservableCollection<PhotoInfoViewModel> Photos { get; private set; }
    public ObservableCollection<PhotoInfoViewModel> PinnedPhotos { get; private set; }
    public bool IsFavorite
    {
        get => Model.IsFavorite;
        set => Model.IsFavorite = value;
    }

    public FiltResultViewModel(FiltResult model)
    {
        Model = model;
        Photos = new ObservableCollection<PhotoInfoViewModel>((model.Photos ?? new List<PhotoInfo>()).Select(p => new PhotoInfoViewModel(p)));
        PinnedPhotos = new ObservableCollection<PhotoInfoViewModel>((model.PinnedPhotos ?? new List<PhotoInfo>()).Select(p => new PhotoInfoViewModel(p)));
    }

    public void UpdateFromModel()
    {
        Photos.Clear();
        PinnedPhotos.Clear();
        foreach (var p in Model.Photos ?? new List<PhotoInfo>()) Photos.Add(new PhotoInfoViewModel(p));
        foreach (var p in Model.PinnedPhotos ?? new List<PhotoInfo>()) PinnedPhotos.Add(new PhotoInfoViewModel(p));
        OnPropertyChanged(nameof(Photos));
        OnPropertyChanged(nameof(PinnedPhotos));
    }

    public void SyncToModel()
    {
        Model.Photos = Photos.Select(vm => vm.Model).ToList();
        Model.PinnedPhotos = PinnedPhotos.Select(vm => vm.Model).ToList();
    }
}
