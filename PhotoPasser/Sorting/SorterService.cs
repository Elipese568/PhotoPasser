using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using PhotoPasser.Helper;
using PhotoPasser.Primitive;

namespace PhotoPasser.Sorting;

/// <summary>
/// 排序器服务的具体实现
/// 使用注册模式实现可扩展性
/// </summary>
public class SorterService : ISorterService
{
    private readonly Dictionary<Type, Dictionary<string, ISortRule<object>>> _rules = new();

    public void RegisterRule<T>(ISortRule<T> rule)
    {
        var type = typeof(T);
        if (!_rules.ContainsKey(type))
        {
            _rules[type] = new Dictionary<string, ISortRule<object>>();
        }
        _rules[type][rule.FieldName] = new ObjectWrapperSortRule<T>(rule);
    }

    public ObservableCollection<T> Sort<T>(ObservableCollection<T> source, SortDescriptor descriptor)
    {
        if (source == null || source.Count == 0)
            return source;

        var type = typeof(T);
        if (!_rules.ContainsKey(type) || !_rules[type].ContainsKey(descriptor.FieldName))
            return source;

        var rule = _rules[type][descriptor.FieldName];

        var ordered = descriptor.Direction == SortOrder.Ascending
            ? source.OrderBy(x => rule.GetSortKey(x))
            : source.OrderByDescending(x => rule.GetSortKey(x));

        return ordered.AsObservable();
    }

    /// <summary>
    /// 包装器，将类型化规则转换为对象规则用于存储
    /// </summary>
    private class ObjectWrapperSortRule<T> : ISortRule<object>
    {
        private readonly ISortRule<T> _inner;

        public ObjectWrapperSortRule(ISortRule<T> inner)
        {
            _inner = inner;
            FieldName = inner.FieldName;
            DisplayName = inner.DisplayName;
        }

        public string FieldName { get; }
        public string DisplayName { get; }

        public IComparable GetSortKey(object item)
        {
            return _inner.GetSortKey((T)item);
        }
    }
}
