namespace PhotoPasser.Primitive;

public enum DisplayView
{
    Trumbull,
    Details,
    Tiles
}

public enum SortOrder
{
    Ascending,
    Descending
}

public enum SortBy
{
    Name,
    Type,
    DateCreated,
    DateModified,
    TotalSize
}
public enum FiltingStatus
{
    Unset,
    Filtered,
    Unfiltered
}
public enum ChangeType
{
    Added,
    Updated,
    Deleted
}