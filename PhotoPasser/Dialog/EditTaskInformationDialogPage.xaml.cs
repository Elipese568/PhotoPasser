using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Windows.Storage.Pickers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace PhotoPasser.Dialog;

public partial class AddTaskDialogViewModel : ObservableObject
{
    [ObservableProperty]
    private string _taskName;
    [ObservableProperty]
    private string _description;
    [ObservableProperty]
    private string _destinationPath;
    [ObservableProperty]
    private bool _copySource;
    [ObservableProperty]
    private DateTime _createAt;

    public bool IsNonCreated { get; set; } = true;
    [RelayCommand]
    public async Task BrowseDestinationPath()
    {
        FolderPicker folderPicker = new FolderPicker(App.GetService<MainWindow>()!.AppWindow.Id);
        // 使用 Resource.resw 中的公共资源
        folderPicker.CommitButtonText = "ChoosePrompt".GetLocalized();
        var folder = await folderPicker.PickSingleFolderAsync();

        if (folder == null)
            return;

        DestinationPath = folder.Path;
    }

    
}
public class ValidationStateChangedEventArgs : EventArgs
{
    public bool IsValid { get; set; }
    public ValidationStateChangedEventArgs(bool isValid)
    {
        IsValid = isValid;
    }
}
/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class EditTaskInformationDialogPage : Page
{
    private bool _validationState;
    public event EventHandler<ValidationStateChangedEventArgs> ValidationStateChanged;
    public AddTaskDialogViewModel ViewModel { get; } = new AddTaskDialogViewModel();
    private FiltTask? _originTask = null;

    public EditTaskInformationDialogPage()
    {
        InitializeComponent();
    }

    public EditTaskInformationDialogPage(FiltTask origin)
    {
        ViewModel.TaskName = origin.Name;
        ViewModel.Description = origin.Description;
        ViewModel.DestinationPath = origin.DestinationPath;
        ViewModel.CopySource = origin.CopySource;
        ViewModel.IsNonCreated = false;
        ViewModel.CreateAt = origin.CreateAt;
        _originTask = origin;
        InitializeComponent();
    }

    private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        bool _new = Path.Exists(ViewModel.DestinationPath) && !string.IsNullOrEmpty(ViewModel.TaskName);
        if (_validationState != _new)
        {
            _validationState = _new;
            ValidationStateChanged?.Invoke(this, new ValidationStateChangedEventArgs(_validationState));
        }
    }
}
