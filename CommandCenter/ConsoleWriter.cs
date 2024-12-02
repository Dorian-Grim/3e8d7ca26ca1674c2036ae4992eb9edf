using System.IO;
using System.Text;
using System.Windows.Controls;

public class TextBoxWriter(TextBox textBox) : TextWriter
{
    private readonly TextBox _textBox = textBox ?? throw new ArgumentNullException(nameof(textBox));

    public override Encoding Encoding => Encoding.UTF8;

    public override void WriteLine(string value)
    {
        // Ensure thread-safe access to the TextBox
        _textBox.Dispatcher.Invoke(() => _textBox.AppendText(value + Environment.NewLine));
        _textBox.Dispatcher.Invoke(() => _textBox.ScrollToEnd());
    }
}
