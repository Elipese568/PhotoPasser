using System;
using PhotoPasser.ViewModels;
using PhotoPasser.Primitive;

namespace PhotoPasser.Sorting;

/// <summary>
/// PhotoInfoViewModel item sorting rules
/// </summary>
public static class PhotoSortRules
{
    public class NameRule : ISortRule<PhotoInfoViewModel>
    {
        public string FieldName => nameof(SortBy.Name);
        public string DisplayName => "PhotoSortName";

        public IComparable GetSortKey(PhotoInfoViewModel item)
            => item.UserName ?? string.Empty;
    }

    public class TypeRule : ISortRule<PhotoInfoViewModel>
    {
        public string FieldName => nameof(SortBy.Type);
        public string DisplayName => "PhotoSortType";

        public IComparable GetSortKey(PhotoInfoViewModel item)
        {
            var fileName = item.UserName ?? string.Empty;
            var lastDotIndex = fileName.LastIndexOf('.');

            if (lastDotIndex >= 0 && lastDotIndex < fileName.Length - 1)
            {
                return fileName.Substring(lastDotIndex + 1).ToLowerInvariant();
            }

            return string.Empty;
        }
    }

    public class DateCreatedRule : ISortRule<PhotoInfoViewModel>
    {
        public string FieldName => nameof(SortBy.DateCreated);
        public string DisplayName => "PhotoSortDateCreated";

        public IComparable GetSortKey(PhotoInfoViewModel item)
            => item.DateCreated;
    }

    public class DateModifiedRule : ISortRule<PhotoInfoViewModel>
    {
        public string FieldName => nameof(SortBy.DateModified);
        public string DisplayName => "PhotoSortDateModified";

        public IComparable GetSortKey(PhotoInfoViewModel item)
            => item.DateModified;
    }

    public class TotalSizeRule : ISortRule<PhotoInfoViewModel>
    {
        public string FieldName => nameof(SortBy.TotalSize);
        public string DisplayName => "PhotoSortTotalSize";

        public IComparable GetSortKey(PhotoInfoViewModel item)
            => item.Size;
    }
}
