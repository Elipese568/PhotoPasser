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