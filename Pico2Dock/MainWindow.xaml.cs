using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Media;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Linq;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using Button = Wpf.Ui.Controls.Button;
using File = System.IO.File;

namespace Pico2Dock
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : FluentWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = this;

            ApplicationThemeManager.Apply(this);
            VersionText.Text = $"Version {GetAppVersion()}";

            ResetAppearance();
            ControlButton(1);
        }

        #region Parameter
        private bool IsCancleProcess = false;
        private ObservableCollection<string> _files = [];
        public ObservableCollection<string> Files
        {
            get
            {
                return _files;
            }
        }
        #endregion

        public static void OnClose()
        {
            Tasks.KillTask();
        }

        #region Drag&Drop

        private void DropBox_DragLeave(object sender, DragEventArgs e)
        {
            Button? button = sender as Button;
            button?.Background = new SolidColorBrush(Color.FromRgb(255, 255, 255));
        }

        private void DropBox_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                foreach (string filePath in files)
                {
                    string fileExtension = System.IO.Path.GetExtension(filePath);
                    if (fileExtension == ".apk")
                        _files.Add(filePath);
                }
            }

            Button? button = sender as Button;
            button?.Background = new SolidColorBrush(Color.FromRgb(255, 255, 255));

            DropBox_UpdateText();
        }

        private void DropBox_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
                Button? button = sender as Button;
                button?.Background = new SolidColorBrush(Color.FromRgb(155, 155, 155));
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }

        private void DropBox_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Android Package (*.apk)|*.apk",
                Multiselect = true
            };

            dlg.ShowDialog();

            foreach (var filePath in dlg.FileNames)
                _files.Add(filePath);

            DropBox_UpdateText();
        }

        private void DropBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
                _files.RemoveAt(DropBox.SelectedIndex);
        }

        private void DropBox_UpdateText()
        {
            if (_files.Count > 0)
                DropBoxButton.Visibility = Visibility.Hidden;
            else
                DropBoxButton.Visibility = Visibility.Visible;

            ControlButton(1);
        }
        #endregion

        #region Button
        private async void StartProcess(object sender, RoutedEventArgs e)
        {
            if (_files.Count > 0)
            {
                ResetAppearance();
                MainTask();
            }
            else
                StatusText.Text = "ERROR: There is no file in process.";
        }

        private void CancleProcess(object sender, RoutedEventArgs e)
        {
            if (!IsCancleProcess)
            {
                IsCancleProcess = true;
                Tasks.KillTask();
            }
        }

        private void ClearFiles(object sender, RoutedEventArgs e)
        {
            _files.Clear();
            ResetAppearance();
            DropBox_UpdateText();
        }

        private void DeleteFiles(object sender, RoutedEventArgs e)
        {
            if (DropBox.SelectedIndex > -1)
                _files.RemoveAt(DropBox.SelectedIndex);
        }

        private void OpenOutput(object sender, RoutedEventArgs e)
        {
            OpenExplorer("patched");
        }
        #endregion

        private void IncressProgressBar(double count)
        {
            StatusProgressBar.Value += 10 / count;
            PercentText.Text = Math.Floor(StatusProgressBar.Value).ToString() + "%";
        }

        private async void MainTask()
        {
            ControlButton(2);

            int ordinal = 0;
            string errorMessage = "";

            foreach (string filePath in _files.ToList())
            {
                DirectoryCleanup();

                string apkName = System.IO.Path.GetFileName(filePath);

                // -------------------- [[ File indicator ]] --------------------
                _files.RemoveAt(ordinal);
                _files.Insert(ordinal, filePath + " 🛠️");
                DropBox.SelectedIndex = ordinal;
                DropBox.ScrollIntoView(DropBox.SelectedItem);

                // -------------------- [[ Start decompiler apk ]] --------------------
                StatusText.Text = $"Current Status: Decompiling {apkName}...";
                IncressProgressBar(_files.Count);

                await Task.Run(() =>
                {
                    errorMessage = Tasks.DecompilerTask(filePath);
                });

                if (IsCancleProcess)
                    goto skipMainTask;

                if (string.IsNullOrEmpty(errorMessage))
                {
                    StatusText.Text = $"Current Status: Decompile {apkName} completed";
                    IncressProgressBar(_files.Count);
                }
                else
                    goto skipMainTask;

                // -------------------- [[ Edit AndroidManifest.xml ]] --------------------
                StatusText.Text = $"Current Status: Modifing AndroidManifest.xml of {apkName}...";
                IncressProgressBar(_files.Count);

                XNamespace android = "http://schemas.android.com/apk/res/android";
                XDocument doc = XDocument.Load("./worker/AndroidManifest.xml");
                XElement root = doc.Root;

                // Traverse all <activity> nodes
                foreach (var activity in root.Descendants("application").Elements("activity"))
                {
                    var metaData = new XElement("meta-data");
                    metaData.SetAttributeValue(android + "name", "pico.vr.position");
                    metaData.SetAttributeValue(android + "value", "near");
                    activity.Add(metaData);
                }

                // Traverse all <activity-alias> nodes
                foreach (var alias in root.Descendants("application").Elements("activity-alias"))
                {
                    var metaData = new XElement("meta-data");
                    metaData.SetAttributeValue(android + "name", "pico.vr.position");
                    metaData.SetAttributeValue(android + "value", "near");
                    alias.Add(metaData);
                }
                doc.Save("./worker/AndroidManifest.xml");

                StatusText.Text = $"Current Status: The file AndroidManifest.xml of {apkName} is modified successfully";
                IncressProgressBar(_files.Count);

                // -------------------- [[ Start compiler apk ]] --------------------
                StatusText.Text = $"Current Status: Compiling {apkName}...";
                IncressProgressBar(_files.Count);

                await Task.Run(() =>
                {
                    errorMessage = Tasks.CompilerTask(apkName);
                });

                if (IsCancleProcess)
                    goto skipMainTask;

                if (string.IsNullOrEmpty(errorMessage))
                {
                    StatusText.Text = $"Current Status: Compile {apkName} completed";
                    IncressProgressBar(_files.Count);
                }
                else
                    goto skipMainTask;

                // -------------------- [[ Start uber apk signer ]] --------------------
                StatusText.Text = $"Current Status: Signing {apkName}...";
                IncressProgressBar(_files.Count);

                await Task.Run(() =>
                {
                    errorMessage = Tasks.SignedTask(apkName);
                });

                if (IsCancleProcess)
                    goto skipMainTask;

                if (string.IsNullOrEmpty(errorMessage))
                {
                    StatusText.Text = $"Current Status: Sign {apkName} completed";
                    IncressProgressBar(_files.Count);
                }
                else
                    goto skipMainTask;

                // -------------------- [[ Rename ]] --------------------
                IncressProgressBar(_files.Count);

                bool signedFile = File.Exists($"./patched/{apkName[..^4]}-aligned-signed.apk");
                if (signedFile)
                {
                    if (File.Exists($"./patched/{apkName[..^4]}-aligned-signed.apk.idsig"))
                        File.Delete($"./patched/{apkName[..^4]}-aligned-signed.apk.idsig");

                    if (Directory.Exists("./patched") && File.Exists($"./patched/PICO_{apkName[..^4]}.apk"))
                    {
                        int count = 1;
                        while (File.Exists($"./patched/PICO_{apkName[..^4]}({count}).apk"))
                            count++;
                        File.Move(
                            $"./patched/{apkName[..^4]}-aligned-signed.apk",
                            $"./patched/PICO_{apkName[..^4]}({count}).apk"
                        );
                    }
                    else
                    {
                        File.Move(
                            $"./patched/{apkName[..^4]}-aligned-signed.apk",
                            $"./patched/PICO_{apkName[..^4]}.apk"
                        );
                    }

                    StatusText.Text = $"Current Status: Process {apkName} is successful";
                }
                else
                {
                    errorMessage = $"Current Status: Process {apkName} is failed";
                    goto skipMainTask;
                }

                // -------------------- [[ File indicator ]] --------------------
                IncressProgressBar(_files.Count);
                _files.RemoveAt(ordinal);
                _files.Insert(ordinal, filePath + " ✔️");

                ordinal++;
            }

        skipMainTask:
            SoundPlayer simpleSound;
            if (IsCancleProcess)
            {
                ControlButton(1);

                StatusText.Text = "Process has been terminated.";
                simpleSound = new(@"c:\Windows\Media\Windows Hardware Remove.wav");
            }
            else if (!string.IsNullOrEmpty(errorMessage))
            {
                ControlButton(1);

                StatusText.Text = errorMessage;
                StatusProgressBar.Foreground = new SolidColorBrush(Colors.Red);
                simpleSound = new(@"c:\Windows\Media\Windows Error.wav");
            }
            else
            {
                ControlButton(3);

                StatusText.Text = $"Current Status: All APK files have been modified.\nYou can install them using the APK files in the patched folder.";
                StatusProgressBar.Foreground = new SolidColorBrush(Colors.Green);
                simpleSound = new(@"c:\Windows\Media\notify.wav");
            }

            IncressProgressBar(_files.Count);
            DirectoryCleanup();

            // Play sound
            simpleSound.Play();
        }

        #region Utils
        private void ControlButton(int state)
        {
            if (state == 1)
            {
                StartButton.IsEnabled = true;
                CancleButton.IsEnabled = false;
            }
            if (state == 2)
            {
                StartButton.IsEnabled = false;
                CancleButton.IsEnabled = true;
            }
            if (state == 3)
            {
                StartButton.IsEnabled = false;
                CancleButton.IsEnabled = false;
            }

            if (_files.Count > 0)
                ClearButton.IsEnabled = true;
            else
                ClearButton.IsEnabled = false;
        }

        private void DirectoryCleanup()
        {
            try
            {
                if (Directory.Exists("./singer"))
                    Directory.Delete("./singer", true);
            }
            catch (Exception ex)
            {
                StatusText.Text = ex.ToString();
            }

            try
            {
                if (Directory.Exists("./worker"))
                    Directory.Delete("./worker", true);
            }
            catch (Exception ex)
            {
                StatusText.Text = ex.ToString();
            }

            Tasks.KillTask();
        }

        private void ResetAppearance()
        {
            IsCancleProcess = false;
            StatusText.Text = "";
            PercentText.Text = "";
            StatusProgressBar.Value = 0;
            StatusProgressBar.Foreground = ApplicationAccentColorManager.PrimaryAccentBrush;

            // Remove file indicator
            foreach (string item in _files.ToList())
            {
                int index = _files.IndexOf(item);

                _files.RemoveAt(index);
                _files.Insert(index, item.Replace(" 🛠️", "").Replace(" ✔️", ""));
            }
        }

        private static string GetAppVersion()
        {
            return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown error reading assembly version";
        }

        private static void OpenExplorer(string filePath)
        {
            string args = string.Format("/e, /select, \"{0}\"", filePath);
            ProcessStartInfo info = new()
            {
                FileName = "explorer",
                Arguments = args
            };
            Process.Start(info);
        }
        #endregion
    }
}