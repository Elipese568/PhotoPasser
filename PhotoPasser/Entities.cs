using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Media;
using PhotoPasser.Helper;
using PhotoPasser.Primitive;
using Windows.UI.ViewManagement;

namespace PhotoPasser;

public partial class FiltTask : ObservableObject
{
    public FiltTask()
    {
        SettingProvider.Instance.ThemeChanged += async (_, _) =>
        {
            MaskBrush = await GetGradientBackground(PresentPhoto);
        };
    }

    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private string _description;

    public bool CopySource { get; set; }

    [ObservableProperty]
    private string _destinationPath;

    [ObservableProperty]
    private Guid _id;

    [ObservableProperty]
    private string _presentPhoto;

    [ObservableProperty]
    private LinearGradientBrush _maskBrush;

	private static async Task<LinearGradientBrush> GetGradientBackground(string photoPath)
	{
        var hind = ColorHelper.AdjustToBackground(await ColorHelper.GetAverageColor(photoPath, 0, 8));
		var visible = ColorHelper.AdjustToBackground(await ColorHelper.GetAverageColor(photoPath, 255, 8));
		// 创建一个新的 LinearGradientBrush
		LinearGradientBrush gradientBrush = new LinearGradientBrush
		{
			StartPoint = new Windows.Foundation.Point(0, 0),
			EndPoint = new Windows.Foundation.Point(0, 1)
		};

		// 设置渐变的停止点
		gradientBrush.GradientStops.Add(new GradientStop
		{
			Color = hind,
			Offset = 0.0
		});

		gradientBrush.GradientStops.Add(new GradientStop
		{
			Color = hind,
			Offset = 0.2
		});

		// 使用主题颜色或者固定颜色作为结束渐变
		gradientBrush.GradientStops.Add(new GradientStop
		{
			Color = visible,
			Offset = 0.623
		});

		gradientBrush.GradientStops.Add(new GradientStop
		{
			Color = visible,
			Offset = 1.0
		});

		// 应用这个渐变画笔到背景
		return gradientBrush;
	}

	protected override async void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        if(e.PropertyName == nameof(PresentPhoto))
        {
            MaskBrush = await GetGradientBackground(PresentPhoto);
        }
        base.OnPropertyChanged(e);
    }
}

public partial class FiltResult : ObservableObject
{
    [ObservableProperty]
    private string _name;
    [ObservableProperty]
    private string _description;
    [ObservableProperty]
    private ObservableCollection<PhotoInfo> _photos;
    [ObservableProperty]
    private ObservableCollection<PhotoInfo> _pinnedPhotos;
    [ObservableProperty]
    private DateTime _date;
    [ObservableProperty]
    private bool _isFavorite;

    public Guid ResultId { get; set; }

    
}

public class RefKeyValuePair<TKey, TValue>
{
    public TKey Key { get; set; }
    public TValue Value { get; set; }
    public RefKeyValuePair(TKey key, TValue value)
    {
        Key = key;
        Value = value;
    }
    public RefKeyValuePair(KeyValuePair<TKey, TValue> struct_kv) : this(struct_kv.Key, struct_kv.Value) { }
}

// ObservableDictionary 实现
public class ObservableDictionary<TKey, TValue> : ObservableCollection<RefKeyValuePair<TKey, TValue>>
{
    public ObservableDictionary(IEnumerable<KeyValuePair<TKey, TValue>> pairs)
    {
        foreach(var kv in pairs)
            Add(new(kv));
    }
    public ObservableDictionary() { }
    public TValue this[TKey key]
    {
        get => TryGetValue(key, out var value) ? value : default;
        set
        {
            var idx = IndexOfKey(key);
            if (idx >= 0)
            {
                this[idx] = new RefKeyValuePair<TKey, TValue>(key, value);
            }    
            else
            {
                Add(new RefKeyValuePair<TKey, TValue>(key, value));
            }
            OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("Item[]"));
        }
    }
    public bool TryGetValue(TKey key, out TValue value)
    {
        foreach (var kv in this)
            if (EqualityComparer<TKey>.Default.Equals(kv.Key, key))
            {
                value = kv.Value;
                return true;
            }
        value = default;
        return false;
    }
    public int IndexOfKey(TKey key)
    {
        for (int i = 0; i < Count; i++)
            if (EqualityComparer<TKey>.Default.Equals(this[i].Key, key))
                return i;
        return -1;
    }

    public bool Remove(TKey key)
    {
        var idx = IndexOfKey(key);
        if (idx >= 0)
        {
            RemoveAt(idx);
            OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("Item[]"));
            return true;
        }
        return false;
    }
}

public partial class TaskDetail : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<FiltResult> _results = new();
    [ObservableProperty]
    private ObservableCollection<PhotoInfo> _photos = new();
    [ObservableProperty]
    private ObservableCollection<PhotoInfo> _pinnedPhotos = new();


}

[JsonConverter(typeof(PhotoInfoJsonConverter))]
public partial class PhotoInfo : ObservableObject
{
    [ObservableProperty]
    private string _path;
    [ObservableProperty]
    private string _userName;
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public DateTime DateCreated { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public DateTime DateModified { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]

    public long Size { get; set; }

    public static async Task<PhotoInfo> Create(string filePath)
    {
        var info = await StorageItemProvider.GetFileInfo(filePath);
        return new()
        {
            Path = filePath,
            UserName = System.IO.Path.GetFileNameWithoutExtension(filePath),
            Size = info.Length,
            DateCreated = info.CreationTime,
            DateModified = info.LastWriteTime
        };
    }
}