using PhotoPasser.Helper;
using PhotoPasser.Models;
using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Windows.Storage;

namespace PhotoPasser.Primitive;

internal class PhotoInfoJsonConverter : JsonConverter<PhotoInfo>
{
    public override PhotoInfo? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        PhotoInfo photoInfo = new PhotoInfo();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                return photoInfo;
            }
            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                string propertyName = reader.GetString()!;
                reader.Read();
                switch (propertyName)
                {
                    case "Path":
                        photoInfo.Path = reader.GetString()!;
                        var fileInfo = new FileInfo(photoInfo.Path);
                        if (fileInfo != null)
                        {
                            photoInfo.DateCreated = fileInfo.CreationTime;
                            photoInfo.DateModified = fileInfo.LastWriteTime;
                            photoInfo.Size = fileInfo.Length;
                        }
                        break;
                    case "UserName":
                        photoInfo.UserName = reader.GetString()!;
                        break;
                }
            }
        }
        return photoInfo;
    }

    public override void Write(Utf8JsonWriter writer, PhotoInfo value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("Path", value.Path);
        writer.WriteString("UserName", value.UserName);
        writer.WriteEndObject();
    }
}