using PhotoPasser.Primitive;
using System;

namespace PhotoPasser.Service;

public class WorkspaceViewState
{
    public int Version { get; set; } = 1;
    public DisplayView CurrentView { get; set; } = DisplayView.Trumbull;
    public SortBy SortBy { get; set; } = SortBy.Name;
    public SortOrder SortOrder { get; set; } = SortOrder.Ascending;
    public string? SearchText { get; set; }
    // store indices or identifiers for selection
    public int[]? SelectedIndices { get; set; }
    // future: scroll offset, column widths, etc.
}
