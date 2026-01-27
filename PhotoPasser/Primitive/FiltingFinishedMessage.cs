using System.Collections.Generic;

namespace PhotoPasser.Primitive;

public class FiltingFinishedMessage
{
    public List<PhotoInfo> FiltedPhotos { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
}
