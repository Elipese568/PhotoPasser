using Microsoft.UI.Xaml;
using System.Threading.Tasks;

namespace PhotoPasser.Service;

public interface IDialogService
{
    // show input dialog and return entered text or null if cancelled
    Task<string?> ShowInputAsync(string title, string message, string defaultText = "", Window? parentWindow = null, bool isPassword = false);

    // show confirmation dialog return true if accepted
    Task<bool> ShowConfirmAsync(string title, object message, string primaryButtonText = "OK", string closeButtonText = "Cancel", Window? parentWindow = null);
}
