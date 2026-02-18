namespace PhotoPasser.Primitive;

/// <summary>
/// 不可变的排序描述符，封装排序字段和方向
/// </summary>
public sealed record SortDescriptor(string FieldName, SortOrder Direction)
{
    public static SortDescriptor Ascending(string fieldName) => new(fieldName, SortOrder.Ascending);
    public static SortDescriptor Descending(string fieldName) => new(fieldName, SortOrder.Descending);

    public SortDescriptor Reverse() => this with { Direction = Direction == SortOrder.Ascending ? SortOrder.Descending : SortOrder.Ascending };
}
