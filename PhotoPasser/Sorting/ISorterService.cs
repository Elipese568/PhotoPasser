using System.Collections.ObjectModel;
using PhotoPasser.Primitive;

namespace PhotoPasser.Sorting;

/// <summary>
/// 通用排序器服务接口
/// </summary>
public interface ISorterService
{
    /// <summary>
    /// 根据描述符对集合进行排序
    /// </summary>
    ObservableCollection<T> Sort<T>(ObservableCollection<T> source, SortDescriptor descriptor);

    /// <summary>
    /// 注册指定类型的排序规则
    /// </summary>
    void RegisterRule<T>(ISortRule<T> rule);
}
