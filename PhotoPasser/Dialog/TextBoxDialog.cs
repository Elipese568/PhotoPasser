using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;

namespace PhotoPasser;

public class TextBoxDialog : ContentDialog
{
    private TextBox _textBox;
    public string Text => _textBox.Text;

    public TextBoxDialog(string title, string label, string defaultText = "", bool allowWhiteSpace = true)
    {
        Title = title;
        PrimaryButtonText = "OK";
        CloseButtonText = "Cancel";
        _textBox = new TextBox { Text = defaultText, Margin = new Thickness(0, 8, 0, 0) };
        Content = new StackPanel
        {
            Children =
            {
                new TextBlock { Text = label },
                _textBox
            }
        };
        if (!allowWhiteSpace)
        {
            _textBox.TextChanged += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(_textBox.Text))
                    IsPrimaryButtonEnabled = false;
                else
                    IsPrimaryButtonEnabled = true;
            };
        }

    }
}
