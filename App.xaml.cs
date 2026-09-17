using System.Configuration;
using System.Data;
using System.Windows;

namespace CavernizeAutomate
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private MainViewModel? _mainViewModel;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            _mainViewModel = new MainViewModel();
            _mainViewModel.Initialise();
            MainWindow = new MainWindow(_mainViewModel);
            MainWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _mainViewModel?.Save();

            base.OnExit(e);
        }
    }

}
