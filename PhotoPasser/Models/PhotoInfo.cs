
using System;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using PhotoPasser.Helper;
using PhotoPasser.Primitive;

namespace PhotoPasser.Models;

[JsonConverter(typeof(PhotoInfoJsonConverter))]
public class PhotoInfo
{
    public string Path { get; set; }
    public string UserName { get; set; }

    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.Always)]
    public long Size { get; set; }
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.Always)]
    public DateTime DateCreated { get; set; }
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.Always)]
    public DateTime DateModified { get; set; }
}
