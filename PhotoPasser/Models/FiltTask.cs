using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace PhotoPasser.Models;

public class FiltTask
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public bool CopySource { get; set; }
    public string DestinationPath { get; set; }
    public string PresentPhoto { get; set; }
}
