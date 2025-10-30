using CommunityToolkit.Mvvm.ComponentModel;
using PhotoPasser.Helper;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace PhotoPasser;

public partial class FiltTask : ObservableObject
{
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
    private DateTime _date;
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