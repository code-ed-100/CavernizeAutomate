using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using static System.Net.WebRequestMethods;

namespace CavernizeAutomate
{
    public partial class MainViewModel : ObservableObject
    {
        // https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ProcessFilesCommand))]
        private string _locationOfCavernizeApp;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(FilesToProcessMsg))]
        [NotifyCanExecuteChangedFor(nameof(ProcessFilesCommand))]
        private ObservableCollection<string> _filesToProcess;

        [ObservableProperty]
        private string _inputFolder;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ProcessFilesCommand))]
        private string _outputFolder;

        [ObservableProperty]
        private ArrayList _outputFileFormatList;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ProcessFilesCommand))]
        private string _outputFileFormat;

        [ObservableProperty]
        private ArrayList _outputRenderFormatList;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ProcessFilesCommand))]
        private string _outputRenderFormat;

        [ObservableProperty]
        private bool _twentyFourBit;

        [ObservableProperty]
        private string _gainDb;

        [ObservableProperty]
        private string _processingMsg;

        [ObservableProperty]
        private string _cavernizeDocsUrl;

        public string FilesToProcessMsg
        {
            get
            {
                return FilesToProcess.Count > 0 ? $"{FilesToProcess.Count} files in folder:\n{InputFolder}" : "None selected";
            }
        }

        public bool CanStartProcessing
        {
            get
            {
                return !string.IsNullOrEmpty(LocationOfCavernizeApp) && FilesToProcess.Count > 0 && !string.IsNullOrEmpty(OutputFolder) &&
                            !string.IsNullOrEmpty(OutputFileFormat) && !string.IsNullOrEmpty(OutputRenderFormat);
            }
        }

        public MainViewModel()
        {
            _locationOfCavernizeApp = string.Empty;
            _filesToProcess = new ObservableCollection<string>();
            _filesToProcess.CollectionChanged += FilesToProcess_CollectionChanged;
            _inputFolder = string.Empty;
            _outputFolder = string.Empty;
            _outputFileFormatList = new ArrayList();
            _outputFileFormat = string.Empty;
            _outputRenderFormatList = new ArrayList();
            _outputRenderFormat = string.Empty;
            _twentyFourBit = true;
            _gainDb = "0";
            _processingMsg = string.Empty;
            _cavernizeDocsUrl = string.Empty;
        }

        private void FilesToProcess_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            // if this is the first file added to the list, update the input folder based on it (all input files must be in the same folder)
            if (FilesToProcess.Count == 1)
                InputFolder = System.IO.Path.GetDirectoryName(FilesToProcess[0]) ?? InputFolder;

            // notify that the message based on the FilesToProcess list will also have changed as a result of this
            OnPropertyChanged("FilesToProcessMsg");

            // notify to reevaluate whether processing can start
            ProcessFilesCommand.NotifyCanExecuteChanged();
        }

        public void Initialise()
        {
            // load some values from somewhere

            LocationOfCavernizeApp = "C:\\Program Files\\VoidX\\Cavernize\\CavernizeGUI.exe";
            // InputFolder
            // OutputFolder
            // OutputFileFormat
            // OutputRenderFormat
            // TwentyFourBit
            // GainDb
            CavernizeDocsUrl = "https://cavern.sbence.hu/cavern/doc.php?p=Cavernize&tab=cl";

            OutputFileFormatList.Add("PCM_LE");
            OutputFileFormatList.Add("PCM_Float");
            OutputRenderFormatList.Add("2.0");
            OutputRenderFormatList.Add("5.1");
            OutputRenderFormatList.Add("7.1");
        }

        public void Save()
        {
            // save these values somewhere

            // LocationOfCavernizeApp
            // InputFolder
            // OutputFolder
            // OutputFileFormat
            // OutputRenderFormat
            // TwentyFourBit
            // GainDb
        }

        [RelayCommand(CanExecute = nameof(CanStartProcessing), IncludeCancelCommand = true)]
        private async Task ProcessFilesAsync(CancellationToken cancelToken)
        {
            // do the processing as a non-UI task

            if (CanStartProcessing)
            {
                try
                {
                    IProgress<string> progress = new Progress<string>(msg => ProcessingMsg = msg);
                    Action processFiles = () => { ProcessFiles(progress, cancelToken); return; };
                    await Task.Factory.StartNew(processFiles, cancelToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);
                }
                catch (OperationCanceledException)
                {
                    ProcessingMsg = "Processing cancelled";
                }
                catch (Exception ex)
                {
                    ProcessingMsg = string.Format("An error occurred during processing:\n{0}", ex.Message);
                }
                finally
                {
                    // show the UI message and reset the buttons
                    if (cancelToken.IsCancellationRequested)
                        ProcessingMsg = "Processing was cancelled";
                    else
                        ProcessingMsg = "Processing complete";

                    // force the reevaluation of CanExecute for all UI elements (e.g. the Browse buttons)
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        private void ProcessFiles(IProgress<string> progress, CancellationToken token)
        {
            for (int i = 0; i < FilesToProcess.Count; i++)
            {
                string inputFileName = System.IO.Path.GetFileName(FilesToProcess[i]);
                string outputFileName = System.IO.Path.Combine(OutputFolder, System.IO.Path.ChangeExtension(inputFileName, ".wav"));

                // update the UI
                progress.Report($"Processing file {i + 1} of {FilesToProcess.Count}:\n{inputFileName}");

                // build the command line argument for Cavernize
                // example command line:
                // "C:\Program Files\VoidX\Cavernize\CavernizeGUI.exe" -i "C:\LocalFiles\Music\Ripping\Blu Ray\Extracted\Flesh + Blood\MKA\Output-001.mka"
                // -f PCM_LE -t "2.0" -f24 --render-gain 2 -o "C:\LocalFiles\Music\Ripping\Blu Ray\Extracted\Flesh + Blood\Rendered\Output-001.wav"
                StringBuilder sbCavernizeCmd = new StringBuilder();
                sbCavernizeCmd.Append("-i \"");
                sbCavernizeCmd.Append(FilesToProcess[i]);
                sbCavernizeCmd.Append($"\" -f {OutputFileFormat} -t \"{OutputRenderFormat}\" ");
                if (TwentyFourBit)
                    sbCavernizeCmd.Append("-f24 ");
                if (!string.IsNullOrWhiteSpace(GainDb) && GainDb != "0")
                    sbCavernizeCmd.Append($"--render-gain {GainDb} ");
                sbCavernizeCmd.Append($"-o \"{outputFileName}\"");

                using (Process cavernizeProcess = new Process())
                {
                    cavernizeProcess.EnableRaisingEvents = false;
                    cavernizeProcess.StartInfo.CreateNoWindow = true;
                    cavernizeProcess.StartInfo.UseShellExecute = false;
                    cavernizeProcess.StartInfo.FileName = LocationOfCavernizeApp;
                    cavernizeProcess.StartInfo.Arguments = sbCavernizeCmd.ToString();
                    cavernizeProcess.Start();
                    cavernizeProcess.WaitForExit();
                }

                // stop processing if cancellation has been requested
                // NOTE!!! A "TaskCanceledException was unhandled
                // by user code" error will be raised here if "Just My Code"
                // is enabled on your computer. On Express editions JMC is
                // enabled and cannot be disabled. The exception is benign.
                // Just press F5 to continue executing your code.
                token.ThrowIfCancellationRequested();
            }
        }
    }
}
