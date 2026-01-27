using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace PhotoPasser.Models;

public class FiltResult
{
    public Guid ResultId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public DateTime Date { get; set; }
    public long TotalSize { get; set; }
    public List<PhotoInfo> Photos { get; set; } = new();
    public List<PhotoInfo> PinnedPhotos { get; set; } = new();
    public bool IsFavorite { get; set; }
}
