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
        private bool IsCancelProcess = false;
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
            Tasks.KillTasks();
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
                        string fileExtension = Path.GetExtension(filePath);
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
            OpenFileDialog dlg = new()
            {
                Filter = "Android Package (*.apk)|*.apk",
                Multiselect = true
            };

            dlg.ShowDialog();

            foreach (string filePath in dlg.FileNames)
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
            {
                string path = DropBox.SelectedValue.ToString();

                if (path.Contains("✔️"))
                {
                    string outPath = Path.GetDirectoryName(path.Replace("✔️ ", string.Empty)) + "\\Pico";
                    Utils.OpenExplorer(outPath);

                }
                else
                    Utils.OpenExplorer(path.Replace("🛠️ ", string.Empty).Replace("✖️ ", string.Empty));
            }
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
            if (Utils.IsJavaInstalled())
            {
                if (_files.Count > 0)
                {
                    ResetAppearance();
                    ControlButton(2);

                    // Remove file indicator except error
                    foreach (string filePath in Files.ToList())
                    {
                        int index = Files.IndexOf(filePath);

                        Files[index] = filePath.Replace("🛠️ ", string.Empty).Replace("✔️ ", string.Empty);
                    }

                    MainTask(Files);
                }
                else
                    ChangeStateText("### ERROR\nThere is no file in process.");
            }
            else
            {
                ChangeStateText($"### ERROR\nUnable to run **Java** on machine.\nPlease install [Java 17](https://download.oracle.com/java/17/archive/jdk-17.0.12_windows-x64_bin.msi) as recommended [here](https://download.oracle.com/java/17/archive/jdk-17.0.12_windows-x64_bin.msi).");
            }
        }

        private void CancelProcess(object sender, RoutedEventArgs e)
        {
            if (!IsCancelProcess)
            {
                ChangeStateText($"### Current Status\nCanceling process please wait...");

                CancelButton.IsEnabled = false;
                IsCancelProcess = true;
                Tasks.KillTasks();
            }
        }

        private void ClearFiles(object sender, RoutedEventArgs e)
        {
            _files.Clear();
            ResetAppearance();
            DropBox_UpdateText();
        }

        private void RemoveFiles(object sender, RoutedEventArgs e)
        {
            if (DropBox.SelectedIndex > -1 && StartButton.IsEnabled)
                _files.RemoveAt(DropBox.SelectedIndex);
        }

        private void OpenContent(object sender, RoutedEventArgs e)
        {
            Utils.OpenExplorer("Pico2Dock.exe");
        }

        private void AppNamePrefix_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            TextBox prefix = sender as TextBox;
            CheckBoxRename.IsEnabled = prefix.Text.Length > 0;
        }
        #endregion

        private void IncressProgressBar(double count, double time = 1)
        {
            StatusProgressBar.Value += ((95 / 6) * time) / count;
            PercentText.Text = Math.Floor(StatusProgressBar.Value).ToString() + "%";
        }

        private async void MainTask(ObservableCollection<string> apkFile)
        {
            string errorMessage = string.Empty;

            ChangeStateText($"### Current Status\nCleaning directory...");
            await Task.Run(DirectoryCleanup);

            foreach (string filePath in apkFile.ToList())
            {
                int index = apkFile.IndexOf(filePath);
                string apkName = Path.GetFileName(filePath);
                string outputDir = Path.GetDirectoryName(filePath) + "/Pico";


                // Replace invalid characters with empty string
                apkName = Regex.Replace(apkName, @"[\x00-\x1f\x7f-\xff\s]", string.Empty);

                // skip is file error from previous task
                if (filePath.Contains("✖️"))
                    continue;

                if (!File.Exists(filePath))
                {
                    errorMessage = $"File **{filePath}** does not exist";
                    continue;
                }


                //?? -------------------- [[ File indicator ]] --------------------
                apkFile[index] = "🛠️ " + filePath;
                DropBox.SelectedIndex = index;
                DropBox.ScrollIntoView(DropBox.SelectedItem);

                //?? -------------------- [[ Start decompiler apk ]] --------------------
                ChangeStateText($"### Current Status\nDecompiling **{apkName}**...");
                IncressProgressBar(apkFile.Count);

                await Task.Run(() =>
                {
                    errorMessage = Tasks.DecompilerTask(filePath);
                });

                if (IsCancelProcess)
                    goto skipMainTask;

                if (!string.IsNullOrEmpty(errorMessage))
                {
                    IncressProgressBar(apkFile.Count, 5);
                    goto skipFile;
                }

                //?? -------------------- [[ Edit AndroidManifest.xml ]] --------------------
                ChangeStateText($"### Current Status\nModifing **AndroidManifest.xml** of **{apkName}**...");
                IncressProgressBar(apkFile.Count);

                bool isHideDock = (bool)SwitchHideDock.IsChecked;
                bool isRePackage = (bool)CheckBoxPackname.IsChecked;
                bool isAdvMode = (bool)CheckBoxPackAdv.IsChecked;
                string namePrefix = AppNamePrefix.Text;
                bool isRename = (bool)CheckBoxRename.IsChecked;
                await Task.Run(() =>
                {
                    XNamespace android = "http://schemas.android.com/apk/res/android";
                    XDocument xmlFile = XDocument.Load("./worker/AndroidManifest.xml");
                    XElement xmlRoot = xmlFile.Root;

                    // Add docked attribute
                    if (true)
                    {
                        XElement metaData = new("meta-data");
                        metaData.SetAttributeValue(android + "name", "pico.vr.position");
                        metaData.SetAttributeValue(android + "value", isHideDock ? "near_dialog" : "near");

                        foreach (XElement activity in xmlRoot.Descendants("application").Elements("activity"))
                            activity.Add(metaData);
                        foreach (XElement alias in xmlRoot.Descendants("application").Elements("activity-alias"))
                            alias.Add(metaData);
                    }

                    // Pico tag
                    if (true)
                    {
                        XElement application = xmlRoot.Element("application");
                        XElement metaData = new("meta-data");

                        metaData.SetAttributeValue(android + "name", "isPUI");
                        metaData.SetAttributeValue(android + "value", "1");
                        application.Add(metaData);

                        metaData = new("meta-data");
                        metaData.SetAttributeValue(android + "name", "pvr.vrshell.mode");
                        metaData.SetAttributeValue(android + "value", "1");
                        application.Add(metaData);

                        metaData = new("meta-data");
                        metaData.SetAttributeValue(android + "name", "pico_permission_dim_show");
                        metaData.SetAttributeValue(android + "value", "false");
                        application.Add(metaData);
                    }

                    // Random package name
                    if (isRePackage)
                    {
                        string packageName = xmlRoot.Attribute("package").Value;
                        string ranPrefix = Utils.GenerateString(6);
                        string newPackageName = $"{packageName}{ranPrefix}";

                        // Change package name
                        xmlRoot.Attribute("package").SetValue(newPackageName);

                        if (isAdvMode)
                        {
                            XAttribute sharedUserId = xmlRoot.Attribute(android + "sharedUserId");
                            if (sharedUserId != null)
                            {
                                string value = sharedUserId.Value;
                                xmlRoot.Attribute(android + "sharedUserId").SetValue(value.Replace(packageName, newPackageName));
                            }
                        }

                        foreach (XElement provider in xmlRoot.Descendants("application").Elements("provider"))
                        {
                            string value = provider.Attribute(android + "authorities").Value;

                            if (value.Contains(packageName))
                                provider.SetAttributeValue(android + "authorities", value.Replace(packageName, newPackageName));
                            else
                                provider.SetAttributeValue(android + "authorities", $"{value}{ranPrefix}");
                        }

                        // Change permission
                        if (true)
                        {
                            foreach (XElement permissions in xmlRoot.Descendants("permission"))
                            {
                                string value = permissions.Attribute(android + "name").Value;
                                if (isAdvMode)
                                    permissions.SetAttributeValue(android + "name", value.Replace(packageName, newPackageName));
                                else
                                    permissions.SetAttributeValue(android + "name", $"{value}{ranPrefix}");
                            }

                            foreach (XElement permissions in xmlRoot.Descendants("uses-permission"))
                            {
                                string value = permissions.Attribute(android + "name").Value;
                                if (isAdvMode)
                                    permissions.SetAttributeValue(android + "name", value.Replace(packageName, newPackageName));
                                else
                                    permissions.SetAttributeValue(android + "name", $"{value}{ranPrefix}");
                            }

                            if (isAdvMode)
                            {
                                foreach (XElement permissions in xmlRoot.Descendants("activity-alias"))
                                {
                                    string value = permissions.Attribute(android + "name").Value;
                                    permissions.SetAttributeValue(android + "name", value.Replace(packageName, newPackageName));
                                }
                            }
                        }
                    }


                    // Change app name
                    if (!string.IsNullOrEmpty(namePrefix))
                    {
                        XElement application = xmlRoot.Element("application");

                        if (isRename)
                            application.Attribute(android + "label").SetValue(namePrefix);
                        else
                        {
                            string stringID = application?.Attribute(android + "label")?.Value?.Replace("@string/", string.Empty) ?? "app_name";

                            foreach (DirectoryInfo dir in new DirectoryInfo("./worker/res").GetDirectories())
                            {
                                if (dir.Name.Contains("values"))
                                {
                                    foreach (FileInfo file in dir.GetFiles("strings.xml"))
                                    {
                                        XDocument stringFile = XDocument.Load(file.FullName);
                                        XElement stringRoot = stringFile.Root;

                                        foreach (XElement srt in stringRoot.Elements("string"))
                                        {
                                            if (srt.Attribute("name").Value.Contains(stringID))
                                            {
                                                srt.SetValue($"{srt.Value}{namePrefix}");
                                            }
                                        }

                                        stringFile.Save(file.FullName);
                                    }
                                }
                            }
                        }
                    }

                    xmlFile.Save("./worker/AndroidManifest.xml");
                });

                //?? -------------------- [[ Start compiler apk ]] --------------------
                ChangeStateText($"### Current Status\nCompiling **{apkName}**...");
                IncressProgressBar(apkFile.Count);

                await Task.Run(() =>
                {
                    errorMessage = Tasks.CompilerTask(apkName);
                });

                if (IsCancelProcess)
                    goto skipMainTask;

                if (!string.IsNullOrEmpty(errorMessage))
                {
                    IncressProgressBar(apkFile.Count, 3);
                    goto skipFile;
                }

                //?? -------------------- [[ Start uber apk signer ]] --------------------
                ChangeStateText($"### Current Status\nSigning **{apkName}**...");
                IncressProgressBar(apkFile.Count);

                await Task.Run(() =>
                {
                    errorMessage = Tasks.SignedTask(apkName, outputDir);
                });

                if (IsCancelProcess)
                    goto skipMainTask;

                if (!string.IsNullOrEmpty(errorMessage))
                {
                    IncressProgressBar(apkFile.Count, 2);
                    goto skipFile;
                }

                //?? -------------------- [[ Rename ]] --------------------
                ChangeStateText($"### Current Status\nFinishing **{apkName}**...");
                IncressProgressBar(apkFile.Count);

                bool signedFile = File.Exists($"{outputDir}/{apkName[..^4]}-aligned-signed.apk");
                if (signedFile)
                {
                    if (File.Exists($"{outputDir}/{apkName[..^4]}-aligned-signed.apk.idsig"))
                        File.Delete($"{outputDir}/{apkName[..^4]}-aligned-signed.apk.idsig");

                    if (Directory.Exists(outputDir) && File.Exists($"{outputDir}/PICO_{apkName[..^4]}.apk"))
                    {
                        int count = 1;
                        while (File.Exists($"{outputDir}/PICO_{apkName[..^4]}({count}).apk"))
                            count++;
                        File.Move(
                            $"{outputDir}/{apkName[..^4]}-aligned-signed.apk",
                            $"{outputDir}/PICO_{apkName[..^4]}({count}).apk"
                        );
                    }
                    else
                    {
                        File.Move(
                            $"{outputDir}/{apkName[..^4]}-aligned-signed.apk",
                            $"{outputDir}/PICO_{apkName[..^4]}.apk"
                        );
                    }
                }
                else
                {
                    errorMessage = $"Unable to compile file {apkName}";

                    IncressProgressBar(apkFile.Count, 1);
                    goto skipFile;
                }

                ChangeStateText($"### Current Status\nCleaning directory...");
                await Task.Run(DirectoryCleanup);

                //?? -------------------- [[ File indicator ]] --------------------
                IncressProgressBar(apkFile.Count);
                apkFile[index] = "✔️ " + filePath;

            skipFile:

                if (!string.IsNullOrEmpty(errorMessage))
                {
                    if (apkFile.Count > 1)
                    {
                        apkFile[index] = "✖️ " + filePath + " 🔘 " + errorMessage;
                    }
                    else
                    {
                        apkFile[index] = "✖️ " + filePath;
                        goto skipMainTask;
                    }
                }
            }

            //?? After task
        skipMainTask:
            SoundPlayer simpleSound;

            ChangeStateText($"### Current Status\nCleaning directory...");
            await Task.Run(DirectoryCleanup);

            if (IsCancelProcess)
            { // Terminate
                PercentText.Text = "Terminated";

                ChangeStateText("### Current Status\nProcess has been terminated.");
                StatusProgressBar.Foreground = new SolidColorBrush(Colors.DarkOrange);
                simpleSound = new(@"c:\Windows\Media\Windows Hardware Fail.wav");
            }
            else if (!string.IsNullOrEmpty(errorMessage))
            { // Error
                PercentText.Text = "Error";

                ChangeStateText($"### ERROR\n{errorMessage}");
                StatusProgressBar.Foreground = new SolidColorBrush(Colors.Red);
                simpleSound = new(@"c:\Windows\Media\Windows Error.wav");
            }
            else
            { // Success
                PercentText.Text = "Successful";

                ChangeStateText($"### Current Status\nAll APK files have been modified.\nYou can install them using the APK files in Pico folder by the same folder as the original file. Double click file in the box above to open in File Explorer.");
                StatusProgressBar.Foreground = new SolidColorBrush(Colors.Green);
                simpleSound = new(@"c:\Windows\Media\Windows Notify Calendar.wav");
            }

            ControlButton(1);
            StatusProgressBar.Value = 100;

            // Play sound
            simpleSound.Play();
        }

        #region Utils
        private void ControlButton(int state)
        {
            if (state == 1)
            {
                StartButton.IsEnabled = true;
                CancelButton.IsEnabled = false;
            }
            if (state == 2)
            {
                StartButton.IsEnabled = false;
                CancelButton.IsEnabled = true;
            }

            if (_files.Count > 0 && !CancelButton.IsEnabled)
                ClearButton.IsEnabled = true;
            else
                ClearButton.IsEnabled = false;
        }

        private void DirectoryCleanup()
        {
            try
            {
                DirectoryInfo singer = new("./singer");

                if (singer.Exists)
                {
                    foreach (FileInfo file in new DirectoryInfo("./singer").GetFiles())
                    {
                        file.Delete();
                    }
                }
            }
            catch (Exception ex)
            {
                ChangeStateText($"```{ex}```");
            }

            try
            {
                DirectoryInfo worker = new("./worker");

                if (worker.Exists)
                {
                    foreach (FileInfo file in worker.GetFiles())
                    {
                        file.Delete();
                    }
                    foreach (string dir in Directory.GetDirectories("./worker"))
                    {
                        Directory.Delete(dir, true);
                    }
                }
            }
            catch (Exception ex)
            {
                ChangeStateText($"```{ex}```");
            }
        }

        private void ResetAppearance()
        {
            ChangeStateText(string.Empty);
            IsCancelProcess = false;
            PercentText.Text = string.Empty;
            StatusProgressBar.Value = 0;
            StatusProgressBar.Foreground = ApplicationAccentColorManager.PrimaryAccentBrush;
        }

        private void DropBoxChangeDeleteButton(object sender, dynamic e)
        {
            if (StartButton.IsEnabled && DropBox.SelectedIndex != -1)
                RemoveButton.IsEnabled = true;
            else
                RemoveButton.IsEnabled = false;
        }

        public void ChangeStateText(string text)
        {
            this.Dispatcher.Invoke(() =>
            {
                StatusText.Content = text;
            });
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