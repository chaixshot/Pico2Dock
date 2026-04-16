using MarkdView;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;
using System.Media;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Linq;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using Button = Wpf.Ui.Controls.Button;

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

            // WPF-UI theme
            ApplyTheme();
            Loaded += (s, e) =>
            {
                SystemThemeWatcher.Watch(this, WindowBackdropType.Mica, true);
                SystemEvents.UserPreferenceChanged += (s, e) => { ApplyTheme(); };
            };
            VersionText.Text = $"Version {Utils.GetAppVersion()}";

            ResetAppearance();
            ControlButton(1);

            DropBox.SizeChanged += DropBoxChangeDeleteButton;
            DropBox.SelectionChanged += DropBoxChangeDeleteButton;

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
            if (StartButton.IsEnabled)
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
            if (e.Key == Key.Delete && DropBox.SelectedIndex > -1 && StartButton.IsEnabled)
                _files.RemoveAt(DropBox.SelectedIndex);
        }

        private void DropBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DropBox.SelectedIndex > -1)
                Utils.OpenExplorer(DropBox.SelectedValue.ToString());

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
                ChangeStateText("### ERROR\nThere is no file in process.");
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
            if (DropBox.SelectedIndex > -1 && StartButton.IsEnabled)
                _files.RemoveAt(DropBox.SelectedIndex);
        }

        private void OpenOutput(object sender, RoutedEventArgs e)
        {
            Utils.OpenExplorer("patched");
        }
        #endregion

        private void IncressProgressBar(double count, int time = 1)
        {
            StatusProgressBar.Value += (10 * time) / count;
            PercentText.Text = Math.Floor(StatusProgressBar.Value).ToString() + "%";
        }

        private async void MainTask()
        {
            ControlButton(2);

            string errorMessage = string.Empty;

            // Remove file indicator except error
            foreach (string filePath in _files.ToList())
            {
                int index = _files.IndexOf(filePath);

                _files.RemoveAt(index);
                _files.Insert(index, filePath.Replace("🛠️ ", string.Empty).Replace("✔️ ", string.Empty));
            }

            foreach (string filePath in _files.ToList())
            {
                DirectoryCleanup();

                int index = _files.IndexOf(filePath);
                string apkName = System.IO.Path.GetFileName(filePath);

                // Replace invalid characters with empty string
                apkName = Regex.Replace(apkName, @"[\x00-\x1f\x7f-\xff\s]", "");

                // skip is file error from previous task
                if (filePath.Contains("✖️"))
                    continue;

                //?? -------------------- [[ File indicator ]] --------------------
                _files.RemoveAt(index);
                _files.Insert(index, "🛠️ " + filePath);
                DropBox.SelectedIndex = index;
                DropBox.ScrollIntoView(DropBox.SelectedItem);

                //?? -------------------- [[ Start decompiler apk ]] --------------------
                ChangeStateText($"### Current Status\nDecompiling **{apkName}**...");
                IncressProgressBar(_files.Count);

                await Task.Run(() =>
                {
                    errorMessage = Tasks.DecompilerTask(filePath);
                });

                if (IsCancleProcess)
                    goto skipMainTask;

                if (string.IsNullOrEmpty(errorMessage))
                {
                    ChangeStateText($"### Current Status\nDecompile **{apkName}** completed");
                    IncressProgressBar(_files.Count);
                }
                else
                {
                    _files.RemoveAt(index);
                    if (_files.Count > 1)
                    {
                        _files.Insert(index, "✖️ " + filePath + " 🔘 " + errorMessage);
                        IncressProgressBar(_files.Count, 4);
                        continue;
                    }
                    else
                    {
                        _files.Insert(index, "✖️ " + filePath);
                        goto skipMainTask;
                    }
                }

                //?? -------------------- [[ Edit AndroidManifest.xml ]] --------------------
                ChangeStateText($"### Current Status\nModifing **AndroidManifest.xml** of **{apkName}**...");
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

                ChangeStateText($"### Current Status\nThe file **AndroidManifest.xml** of **{apkName}** is modified successfully");
                IncressProgressBar(_files.Count);

                //?? -------------------- [[ Start compiler apk ]] --------------------
                ChangeStateText($"### Current Status\nCompiling **{apkName}**...");
                IncressProgressBar(_files.Count);

                await Task.Run(() =>
                {
                    errorMessage = Tasks.CompilerTask(apkName);
                });

                if (IsCancleProcess)
                    goto skipMainTask;

                if (string.IsNullOrEmpty(errorMessage))
                {
                    ChangeStateText($"### Current Status\nCompile **{apkName}** completed");
                    IncressProgressBar(_files.Count);
                }
                else
                {
                    _files.RemoveAt(index);
                    if (_files.Count > 1)
                    {
                        _files.Insert(index, "✖️ " + filePath + " 🔘 " + errorMessage);
                        IncressProgressBar(_files.Count, 3);
                        continue;
                    }
                    else
                    {
                        _files.Insert(index, "✖️ " + filePath);
                        goto skipMainTask;
                    }
                }

                //?? -------------------- [[ Start uber apk signer ]] --------------------
                ChangeStateText($"### Current Status\nSigning **{apkName}**...");
                IncressProgressBar(_files.Count);

                await Task.Run(() =>
                {
                    errorMessage = Tasks.SignedTask(apkName);
                });

                if (IsCancleProcess)
                    goto skipMainTask;

                if (string.IsNullOrEmpty(errorMessage))
                {
                    ChangeStateText($"### Current Status\nSign **{apkName}** completed");
                    IncressProgressBar(_files.Count);
                }
                else
                {
                    _files.RemoveAt(index);
                    if (_files.Count > 1)
                    {
                        _files.Insert(index, "✖️ " + filePath + " 🔘 " + errorMessage);
                        IncressProgressBar(_files.Count, 2);
                        continue;
                    }
                    else
                    {
                        _files.Insert(index, "✖️ " + filePath);
                        goto skipMainTask;
                    }
                }

                //?? -------------------- [[ Rename ]] --------------------
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

                    ChangeStateText($"### Current Status\nProcess **{apkName}** is successful");
                }
                else
                {
                    errorMessage = $"### ERROR\nUnable to compile file {apkName}";

                    _files.RemoveAt(index);
                    if (_files.Count > 1)
                    {
                        _files.Insert(index, "✖️ " + filePath + " 🔘 " + errorMessage);
                        IncressProgressBar(_files.Count, 1);
                        continue;
                    }
                    else
                    {
                        _files.Insert(index, "✖️ " + filePath);
                        goto skipMainTask;
                    }
                }

                //?? -------------------- [[ File indicator ]] --------------------
                IncressProgressBar(_files.Count);
                _files.RemoveAt(index);
                _files.Insert(index, "✔️ " + filePath);
            }

            //?? After task
        skipMainTask:
            SoundPlayer simpleSound;

            StatusProgressBar.Value = 100;

            if (IsCancleProcess)
            { // Terminate
                PercentText.Text = "Terminated";

                ChangeStateText("Process has been terminated.");
                StatusProgressBar.Foreground = new SolidColorBrush(Colors.DarkOrange);
                simpleSound = new(@"c:\Windows\Media\Windows Hardware Fail.wav");
            }
            else if (!string.IsNullOrEmpty(errorMessage))
            { // Error
                PercentText.Text = "Error";

                ChangeStateText(errorMessage);
                StatusProgressBar.Foreground = new SolidColorBrush(Colors.Red);
                simpleSound = new(@"c:\Windows\Media\Windows Error.wav");
            }
            else
            { // Success
                PercentText.Text = "Successful";

                ChangeStateText($"### Current Status\nAll APK files have been modified.\nYou can install them using the APK files in the **patched** folder.");
                StatusProgressBar.Foreground = new SolidColorBrush(Colors.Green);
                simpleSound = new(@"c:\Windows\Media\Windows Notify Calendar.wav");
            }

            ControlButton(1);
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

            if (_files.Count > 0 && !CancleButton.IsEnabled)
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
                ChangeStateText($"```{ex}```");
            }

            try
            {
                if (Directory.Exists("./worker"))
                    Directory.Delete("./worker", true);
            }
            catch (Exception ex)
            {
                ChangeStateText($"```{ex}```");
            }

            Tasks.KillTask();
        }

        private void ResetAppearance()
        {
            ChangeStateText(string.Empty);
            IsCancleProcess = false;
            PercentText.Text = string.Empty;
            StatusProgressBar.Value = 0;
            StatusProgressBar.Foreground = ApplicationAccentColorManager.PrimaryAccentBrush;
        }

        private void DropBoxChangeDeleteButton(object sender, dynamic e)
        {
            if (StartButton.IsEnabled && DropBox.SelectedIndex != -1)
                DeleteButton.IsEnabled = true;
            else
                DeleteButton.IsEnabled = false;
        }

        private void ChangeStateText(string text)
        {
            StatusText.Content = text;
        }

        public static void ApplyTheme()
        {
            string theme = ApplicationThemeManager.GetSystemTheme().ToString();

            ThemeManager.ApplyTheme(theme == "Light" ? MarkdView.Enums.ThemeMode.Light : MarkdView.Enums.ThemeMode.Dark);
            ApplicationThemeManager.ApplySystemTheme();
        }
        #endregion
    }
}