using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using HarborKiosk.App.Services;
using HarborKiosk.App.ViewModels;

namespace HarborKiosk.App;

public partial class MainWindow : Window
{
    private readonly NavigationService _navigation;
    private readonly KioskOptions _options;
    private bool _allowClose;

    public MainWindow(NavigationService navigation, KioskOptions options)
    {
        InitializeComponent();
        _navigation = navigation;
        _options = options;
        DataContext = navigation;

        if (options.KioskMode)
        {
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            WindowState = WindowState.Maximized;
            Topmost = true;
            Cursor = Cursors.None;
        }
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (_options.KioskMode && !_allowClose)
        {
            e.Cancel = true;
        }

        base.OnClosing(e);
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        if (_options.KioskMode && e.Key == Key.F12 && Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
        {
            _allowClose = true;
            Close();
            return;
        }

        if (_navigation.CurrentViewModel is IKeypadInput keypad)
        {
            if (ToDigit(e.Key) is char digit)
            {
                keypad.AppendDigit(digit);
                e.Handled = true;
            }
            else if (e.Key == Key.Back)
            {
                keypad.DeleteDigit();
                e.Handled = true;
            }
        }

        base.OnPreviewKeyDown(e);
    }

    private static char? ToDigit(Key key) => key switch
    {
        >= Key.D0 and <= Key.D9 => (char)('0' + (key - Key.D0)),
        >= Key.NumPad0 and <= Key.NumPad9 => (char)('0' + (key - Key.NumPad0)),
        _ => null,
    };
}
