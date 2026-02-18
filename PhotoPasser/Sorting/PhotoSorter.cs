using System.Collections.ObjectModel;
using PhotoPasser.ViewModels;
using PhotoPasser.Primitive;

namespace PhotoPasser.Sorting;

/// <summary>
/// PhotoInfoViewModel items sorting convenience API
/// </summary>
public sealed class PhotoSorter
{
    private readonly ISorterService _sorter;

    public PhotoSorter(ISorterService sorter)
    {
        _sorter = sorter;
    }

    public ObservableCollection<PhotoInfoViewModel> Sort(
        ObservableCollection<PhotoInfoViewModel> photos,
        SortBy sortBy,
        SortOrder order)
    {
        var fieldName = sortBy.ToString();
        var descriptor = new SortDescriptor(fieldName, order);
        return _sorter.Sort(photos, descriptor);
    }
}
