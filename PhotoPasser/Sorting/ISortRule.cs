using System;

namespace PhotoPasser.Sorting;

/// <summary>
/// 单个排序规则的接口
/// </summary>
/// <typeparam name="T">要排序的项目类型</typeparam>
public interface ISortRule<T>
{
    /// <summary>
    /// 排序字段的唯一标识符
    /// </summary>
    string FieldName { get; }

    /// <summary>
    /// 获取项目的排序键
    /// </summary>
    IComparable GetSortKey(T item);

    /// <summary>
    /// UI 显示的名称
    /// </summary>
    string DisplayName { get; }
}
