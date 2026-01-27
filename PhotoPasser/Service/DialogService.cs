using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;

namespace PhotoPasser.Service;

public class DialogService : IDialogService
{
    public async Task<string?> ShowInputAsync(string title, string message, string defaultText = "", Window? parentWindow = null, bool isPassword = false)
    {
        var dialog = new TextBoxDialog(title, message, defaultText, isPassword)
        {
            XamlRoot = parentWindow?.Content.XamlRoot ?? App.GetService<MainWindow>()!.Content.XamlRoot
        };
        var res = await dialog.ShowAsync();
        return res == ContentDialogResult.Primary ? dialog.Text : null;
    }

    public async Task<bool> ShowConfirmAsync(string title, object message, string primaryButtonText = "OK", string closeButtonText = "Cancel", Window? parentWindow = null)
    {
        var cd = new ContentDialog()
        {
            Title = title,
            Content = message,
            PrimaryButtonText = primaryButtonText,
            CloseButtonText = closeButtonText,
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = parentWindow?.Content.XamlRoot ?? App.GetService<MainWindow>()!.Content.XamlRoot
        };
        var r = await cd.ShowAsync();
        return r == ContentDialogResult.Primary;
    }
}
