using PhotoPasser.Service;
using PhotoPasser.ViewModels;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace PhotoPasser.Primitive;

public class ResultViewNavigationParameter
{
    public ITaskDetailPhysicalManagerService TaskDpmService { get; set; }
    public FiltResultViewModel Result { get; set; }
}
