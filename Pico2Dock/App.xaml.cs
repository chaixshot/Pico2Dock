using System.Windows;

namespace Pico2Dock
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static MainWindow mainWindow;

        protected override void OnStartup(StartupEventArgs e)
        {
            mainWindow = new();
            mainWindow.Show();
            mainWindow.Activate();
        }

        private void AppExit(object sender, ExitEventArgs e)
        {
            Pico2Dock.MainWindow.OnClose();
        }
    }

}
