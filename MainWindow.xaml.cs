using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CavernizeAutomate
{
    // https://stackoverflow.com/questions/1043918/open-file-dialog-mvvm/23303267#23303267
    // https://github.com/BionicCodeStackoverflow/mvvm-open-file-dialog-example

    // see answer from dotNet June 28 2019 withj vote=2 for IdialogService example code??
    // https://stackoverflow.com/questions/454868/handling-dialogs-in-wpf-with-mvvm

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainViewModel MainViewModel { get => (MainViewModel)DataContext; }

        public static readonly RoutedCommand SelectCavernizeAppCommand = new RoutedCommand();
        public static readonly RoutedCommand SelectFilesToProcessCommand = new RoutedCommand();
        public static readonly RoutedCommand SelectOutputFolderCommand = new RoutedCommand();

        public MainWindow(MainViewModel mainViewModel)
        {
            InitializeComponent();
            DataContext = mainViewModel;
        }

        private void InputCommands_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            // set to true if processing flag == false
            if (MainViewModel != null)
                e.CanExecute = !MainViewModel.ProcessFilesCommand.IsRunning;
            else
                e.CanExecute = false;
        }

        private void SelectCavernizeAppCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "EXE files (*.exe)|*.exe|All files (*.*)|*.*";

            if (System.IO.File.Exists(MainViewModel.LocationOfCavernizeApp))
            {
                ofd.InitialDirectory = System.IO.Path.GetDirectoryName(MainViewModel.LocationOfCavernizeApp);
            }
            else
            {
                ofd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            }

            if (ofd.ShowDialog() == true)
            {
                MainViewModel.LocationOfCavernizeApp = ofd.FileName;
            }
        }

        private void SelectFilesToProcess_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "MKA files (*.mka)|*.mka|MKV files (*.mkv)|*.mkv|All files (*.*)|*.*";
            ofd.Multiselect = true;

            if (Directory.Exists(MainViewModel.InputFolder))
            {
                ofd.InitialDirectory = MainViewModel.InputFolder;
            }

            if (ofd.ShowDialog() == true)
            {
                MainViewModel.FilesToProcess.Clear();

                for (int i = 0; i < ofd.FileNames.Length; i++)
                {
                    MainViewModel.FilesToProcess.Add(ofd.FileNames[i]);
                }
            }
        }

        private void SelectOutputFolder_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            OpenFolderDialog ofd = new OpenFolderDialog();
            ofd.Title = "Select the output folder";
            ofd.Multiselect = false;
            if (Directory.Exists(MainViewModel.OutputFolder))
            {
                ofd.InitialDirectory = MainViewModel.OutputFolder;
            }

            if (ofd.ShowDialog() == true)
            {
                MainViewModel.OutputFolder = ofd.FolderName;
            }
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                Hyperlink? source = sender as Hyperlink;

                if (source != null)
                {
                    using (Process p = new Process())
                    {
                        p.StartInfo.FileName = source.NavigateUri.ToString();
                        p.StartInfo.UseShellExecute = true;
                        p.Start();
                    }
                }

                e.Handled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Could not open web page:\n\n{0}", ex.Message), "Open Cavernize Documentation", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}