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
                Utils.OpenExplorer(DropBox.SelectedValue.ToString().Replace("🛠️ ", string.Empty).Replace("✔️ ", string.Empty).Replace("✖️ ", string.Empty));
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
                Tasks.KillTasks();
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

        private void AppNamePrefix_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            TextBox prefix = sender as TextBox;
            CheckBoxRename.IsEnabled = prefix.Text.Length > 0;
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
                ChangeStateText($"### Current Status\nCleaning directory...");
                await Task.Run(DirectoryCleanup);

                int index = _files.IndexOf(filePath);
                string apkName = System.IO.Path.GetFileName(filePath);

                // Replace invalid characters with empty string
                apkName = Regex.Replace(apkName, @"[\x00-\x1f\x7f-\xff\s]", string.Empty);

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

                bool isDockMode = (bool)SwitchDockMode.IsChecked;
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
                        metaData.SetAttributeValue(android + "value", isDockMode ? "near" : "near_dialog");

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

            await Task.Run(DirectoryCleanup);

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
                foreach (FileInfo file in new DirectoryInfo("./singer").GetFiles())
                {
                    file.Delete();
                }
            }
            catch (Exception ex)
            {
                ChangeStateText($"```{ex}```");
            }

            try
            {
                foreach (FileInfo file in new DirectoryInfo("./worker").GetFiles())
                {
                    file.Delete();
                }
                foreach (string dir in Directory.GetDirectories("./worker"))
                {
                    Directory.Delete(dir, true);
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