
using PhotoPasser.Helper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Storage;

namespace PhotoPasser.Service.Primitive;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class JsonStorageFileAttribute : Attribute
{
    public string FileName { get; set; }
    public JsonStorageFileAttribute()
    {
    }
}

public class JsonSeriailizingServiceBase<TData> : LifetimeBasedService
{
    protected StorageFile _storage;
    protected TData _data;

    protected override void OnStart()
    {
        _storage = ApplicationData.Current.LocalFolder.CreateFileAsync(GetType().GetCustomAttribute<JsonStorageFileAttribute>().FileName, CreationCollisionOption.OpenIfExists).Sync();
        using var readStream = _storage.OpenStreamForReadAsync().Sync();
        if (readStream.Length == 0)
        {
            _data = Activator.CreateInstance<TData>();
        }
        else
        {
            try
            {
                _data = JsonSerializer.Deserialize<TData>(readStream);
            }
            catch
            {
                _data = Activator.CreateInstance<TData>();
            }
        }
    }

    protected override void OnExit()
    {
        if(_storage == null)
            return;
        _storage.DeleteAsync().Sync();
        _storage = ApplicationData.Current.LocalFolder.CreateFileAsync(GetType().GetCustomAttribute<JsonStorageFileAttribute>().FileName, CreationCollisionOption.OpenIfExists).Sync();
        using var serializedStream = _storage.OpenStreamForWriteAsync().Sync();
        try
        {
            JsonSerializer.Serialize(serializedStream, _data);
        }
        catch (Exception ex)
        {
            // Handle serialization exception if needed
            System.Diagnostics.Debug.WriteLine($"Serialization error: {ex.Message}");
        }
    }

    public string GetStorageFilePath()
    {
        return _storage.Path;
    }
}
