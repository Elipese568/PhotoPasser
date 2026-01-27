using PhotoPasser.Service;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace PhotoPasser.Primitive;

public class ResultViewNavigationParameter
{
    public ITaskDetailPhysicalManagerService TaskDpmService { get; set; }
    public FiltResult NavigatingResult { get; set; }
}
