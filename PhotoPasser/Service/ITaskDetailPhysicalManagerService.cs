using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoPasser.Service;

public interface ITaskDetailPhysicalManagerService : IDisposable
{
    Task<TaskDetail> InitializeAsync(FiltTask task);

    Task<Uri> CopySourceAsync(Uri source);
}
