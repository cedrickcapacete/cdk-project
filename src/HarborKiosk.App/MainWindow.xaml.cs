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

    public MainWindow(NavigationService navigation, KioskOptions options, KioskShell shell)
    {
        InitializeComponent();
        _navigation = navigation;
        _options = options;
        DataContext = navigation;
        shell.ExitRequested += (_, _) => ForceClose();

        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;

        if (options.KioskMode)
        {
            Topmost = true;
            Cursor = Cursors.None;
            Left = 0;
            Top = 0;
            Width = SystemParameters.PrimaryScreenWidth;
            Height = SystemParameters.PrimaryScreenHeight;
        }
        else
        {
            Width = 540;
            Height = 960;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
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
        var ctrlShift = Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift);
        if (ctrlShift && e.Key == Key.F12)
        {
            ForceClose();
            return;
        }

        if (ctrlShift && e.Key == Key.A && _navigation.CurrentViewModel is WelcomeViewModel welcome)
        {
            welcome.OpenAdminCommand.Execute(null);
            e.Handled = true;
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

    private void ForceClose()
    {
        _allowClose = true;
        Close();
    }

    private static char? ToDigit(Key key) => key switch
    {
        >= Key.D0 and <= Key.D9 => (char)('0' + (key - Key.D0)),
        >= Key.NumPad0 and <= Key.NumPad9 => (char)('0' + (key - Key.NumPad0)),
        _ => null,
    };
}
