namespace PhotoPasser.Primitive;

public enum ItemPanelBackgroundThemeResource
{
    /// <summary>
    /// LayerOnMicaBaseAltFillColorDefaultBrush - Mica 背景上的层填充色
    /// </summary>
    LayerOnMicaBaseAltFillColorDefaultState,

    /// <summary>
    /// LayerFillColorDefault - 基础层填充色
    /// </summary>
    LayerFillColorDefaultState
}

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

public enum TaskSortBy
{
    Name,              // 按名称排序
    Description,       // 按描述排序
    CreateAt,          // 按创建时间排序
    RecentlyVisitAt    // 按最近访问时间排序
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