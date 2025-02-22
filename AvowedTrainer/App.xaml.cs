using System.Globalization;
using System.Windows;

namespace AvowedTrainer;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private static Mutex? _mutex = null;
    private const string MutexName = "AvowedTrainer.Mutex";

    protected override void OnStartup(StartupEventArgs e)
    {
        bool createdNew;
        _mutex = new Mutex(true, MutexName, out createdNew);

        if (!createdNew)
        {
            // Another instance is already running
            MessageBox.Show("Another instance of this application is already running!", "Avowed Trainer - Already running",
                MessageBoxButton.OK, MessageBoxImage.Exclamation);

            // Shutdown this instance
            _mutex = null;
            Current.Shutdown();
            return;
        }

        base.OnStartup(e);

        // Set culture globally
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

        // Also enforce UI culture explicitly
        Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
        Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_mutex != null)
        {
            _mutex.ReleaseMutex();
            _mutex.Dispose();
        }
        base.OnExit(e);
    }
}
