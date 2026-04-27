using MarkdView;
using Microsoft.VisualBasic.FileIO;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Media;
using System.Runtime.CompilerServices;
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
    public partial class MainWindow : FluentWindow, INotifyPropertyChanged
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
            ChangeButtonState();
        }

        #region Parameter
        private bool isProcessCancel = false;
        private bool isProcessRunning = false;
        private readonly ObservableCollection<string> APKFiles = [];
        private readonly ObservableCollection<string> APKFilesOut = [];

        public event PropertyChangedEventHandler? PropertyChanged;

        public ObservableCollection<string> Files
        {
            get
            {
                return APKFiles;
            }
        }

        public bool IsProcessNotRunning
        {
            get
            {
                return !isProcessRunning;
            }
            set
            {
                OnPropertyChanged();
            }
        }

        public void OnPropertyChanged([CallerMemberName] string? propName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
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
            if (!isProcessRunning)
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                    foreach (string filePath in files)
                    {
                        string fileExtension = Path.GetExtension(filePath);
                        if (Regex.IsMatch(fileExtension, @".*\.x?apk[ms]?$"))
                        {
                            APKFiles.Add(filePath);
                            APKFilesOut.Add(filePath);
                        }
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
                Filter = @"Android Package (*.apk)|*.apk;*.xapk;*.apkm;*.apks",
                Multiselect = true
            };

            dlg.ShowDialog();

            foreach (string filePath in dlg.FileNames)
            {
                APKFiles.Add(filePath);
                APKFilesOut.Add(filePath);
            }

            DropBox_UpdateText();
        }

        private void DropBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Delete && DropBox.SelectedIndex > -1 && !isProcessRunning)
                APKFiles.RemoveAt(DropBox.SelectedIndex);
        }

        private void DropBox_UpdateText()
        {
            if (APKFiles.Count > 0)
                DropBoxButton.Visibility = Visibility.Hidden;
            else
                DropBoxButton.Visibility = Visibility.Visible;

            ChangeButtonState();
        }

        private void Contextmenu_Open(object sender, RoutedEventArgs e)
        {
            int index = DropBox.SelectedIndex;

            if (index > -1)
                Utils.OpenExplorer(APKFilesOut[index]);
        }

        private void Contextmenu_Remove(object sender, RoutedEventArgs e)
        {
            int index = DropBox.SelectedIndex;

            if (index > -1 && !isProcessRunning)
                APKFiles.RemoveAt(index);
        }

        private async void Contextmenu_Delete(object sender, RoutedEventArgs e)
        {
            int index = DropBox.SelectedIndex;

            if (index > -1 && !isProcessRunning)
            {
                string apkPath = APKFiles[index];
                string apkOutPath = APKFilesOut[index];
                bool isConverted = apkPath.Contains("✔️");
                string apkTargetPath = isConverted ? apkOutPath : apkPath;

                bool isYes = await DialogBox.Show("Move to Recycle Bin?", apkTargetPath, "Yes", "No");

                if (isYes)
                {
                    FileSystem.DeleteFile(apkTargetPath, UIOption.AllDialogs, RecycleOption.SendToRecycleBin);
                    APKFiles.RemoveAt(index);
                }
            }
        }

        #endregion

        #region Button
        private async void StartProcess(object sender, RoutedEventArgs e)
        {
            if (Utils.IsJavaInstalled())
            {
                ResetAppearance();

                // Remove file indicator except error
                foreach (string filePath in APKFiles.ToList())
                {
                    int index = APKFiles.IndexOf(filePath);

                    APKFiles[index] = filePath.Replace("🛠️ ", string.Empty).Replace("✔️ ", string.Empty);
                }

                isProcessRunning = true;
                IsProcessNotRunning = true;
                MainTask(APKFiles);
                ChangeButtonState();
            }
            else
            {
                ChangeStateText($"### ERROR\nUnable to run **Java** on machine.\nPlease install [Java 17](https://download.oracle.com/java/17/archive/jdk-17.0.12_windows-x64_bin.msi) as recommended [here](https://download.oracle.com/java/17/archive/jdk-17.0.12_windows-x64_bin.msi).");
            }
        }

        private void CancelProcess(object sender, RoutedEventArgs e)
        {
            if (!isProcessCancel)
            {
                ChangeStateText($"### Current Status\nCanceling process please wait...");

                isProcessCancel = true;
                ChangeButtonState();
                Tasks.KillTasks();
            }
        }

        private void ClearFiles(object sender, RoutedEventArgs e)
        {
            APKFiles.Clear();
            APKFilesOut.Clear();
            ResetAppearance();
            DropBox_UpdateText();
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

        private async void MainTask(ObservableCollection<string> apkFiles)
        {
            string errorMessage = string.Empty;
            string namePrefix = AppNamePrefix.Text;
            bool isHideDock = (bool)SwitchHideDock.IsChecked;
            bool isRePackage = (bool)CheckBoxPackname.IsChecked;
            bool isAdvMode = (bool)CheckBoxPackAdv.IsChecked;
            bool isRename = (bool)CheckBoxRename.IsChecked;

            foreach (string file in apkFiles.ToList())
            {
                // skip is file error from previous task
                if (file.Contains("✖️"))
                    continue;

                int index = apkFiles.IndexOf(file);
                bool isApkEditor = false;
                ProgressBar progressBar = new(apkFiles.Count, 5);

            startFile:

                ChangeStateText($"### Current Status\nCleaning directory...");
                await Task.Run(Utils.DirectoryCleanup);

                errorMessage = string.Empty;

                string filePath = Path.GetDirectoryName(file);
                FileInfo apkFile = new(file);
                FileInfo dirUnsign = new(".\\Unsign");
                FileInfo dirMerger = new(".\\Merger");
                FileInfo dirOut = new(filePath + "\\Pico");
                FileInfo dirApkOut = new($"{dirOut}\\Pico_{apkFile.Name}");
                FileInfo dirApkUnsing = new($"{dirUnsign}\\Pico_{apkFile.Name}");

                //?? -------------------- [[ File indicator ]] --------------------
                apkFiles[index] = "🛠️ " + file;
                DropBox.SelectedIndex = index;
                DropBox.ScrollIntoView(DropBox.SelectedItem);

                //?? -------------------- [[ Check file access ]] --------------------
                if (!apkFile.Exists)
                {
                    errorMessage = $"Can't access file **{apkFile.FullName}**";
                    continue;
                }

                //?? -------------------- [[ Convert APKM to APK ]] --------------------
                if (Regex.IsMatch(apkFile.Name, @".*\.(xapk|apkm|apks)$"))
                {
                    progressBar.Step += 1;
                    progressBar.Increase();

                    errorMessage = await Task.Run(() => Tasks.ApkEditor.Merger(apkFile));

                    isApkEditor = true;
                    apkFile = new($"{dirMerger}\\{apkFile.Name.Replace(apkFile.Extension, ".apk")}");
                    dirApkOut = new($"{dirOut}\\Pico_{apkFile.Name}");
                    dirApkUnsing = new($"{dirUnsign}\\Pico_{apkFile.Name}");

                    if (isProcessCancel)
                        goto skipMainTask;
                    else if (!string.IsNullOrEmpty(errorMessage))
                    {
                        progressBar.Increase(5);

                        goto skipFile;
                    }
                }

                //?? -------------------- [[ Rename ]] --------------------
                if (dirApkOut.Exists)
                {
                    int count = 1;
                    while (new FileInfo($"{dirApkOut.FullName[..^4]} ({count}).apk").Exists)
                    {
                        count++;
                    }
                    dirApkOut = new($"{dirApkOut.FullName[..^4]} ({count}).apk");
                    dirApkUnsing = new($"{dirApkUnsing.FullName[..^4]} ({count}).apk");
                }

                //?? -------------------- [[ Start decompiler apk ]] --------------------
                progressBar.Increase();

                if (isApkEditor)
                    errorMessage = await Task.Run(() => Tasks.ApkEditor.Decompiler(apkFile));
                else
                    errorMessage = await Task.Run(() => Tasks.ApkTool.Decompiler(apkFile));

                if (isProcessCancel)
                    goto skipMainTask;
                else if (!string.IsNullOrEmpty(errorMessage))
                {
                    if (isApkEditor)
                    {
                        progressBar.Increase(4);

                        goto skipFile;
                    }
                    else
                    {
                        isApkEditor = true;
                        progressBar.Step += 1;

                        goto startFile;
                    }
                }

                //?? -------------------- [[ Edit AndroidManifest.xml ]] --------------------
                ChangeStateText($"### Current Status\nModifing **AndroidManifest.xml** of **{apkFile.Name}**...");
                progressBar.Increase();

                try
                {
                    XNamespace android = "http://schemas.android.com/apk/res/android";
                    XDocument xmlFile = XDocument.Load(".\\Worker\\AndroidManifest.xml");
                    XElement xmlRoot = xmlFile.Root;
                    XElement application = xmlRoot.Element("application");

                    // Add docked attribute
                    if (true)
                    {
                        XElement metaDataVrPosition = new("meta-data");
                        metaDataVrPosition.SetAttributeValue(android + "name", "pico.vr.position");
                        metaDataVrPosition.SetAttributeValue(android + "value", isHideDock ? "near_dialog" : "near");

                        XElement metaDataVrMode = new("meta-data");
                        metaDataVrMode.SetAttributeValue(android + "name", "pvr.2dtovr.mode");
                        metaDataVrMode.SetAttributeValue(android + "value", "6");

                        foreach (XElement activity in xmlRoot.Descendants("application").Elements("activity"))
                        {
                            activity.Add(metaDataVrPosition);
                            activity.Add(metaDataVrMode);
                        }
                        foreach (XElement alias in xmlRoot.Descendants("application").Elements("activity-alias"))
                        {
                            alias.Add(metaDataVrPosition);
                            alias.Add(metaDataVrMode);
                        }
                    }

                    // Pico tag
                    if (true)
                    {
                        XElement metaData = new("meta-data");

                        metaData.SetAttributeValue(android + "name", "isPUI");
                        metaData.SetAttributeValue(android + "value", "1");
                        application.Add(metaData);

                        metaData = new("meta-data");
                        metaData.SetAttributeValue(android + "name", "pvr.vrshell.mode");
                        metaData.SetAttributeValue(android + "value", "1");
                        application.Add(metaData);

                        metaData = new("meta-data");
                        metaData.SetAttributeValue(android + "name", "com.pvr.hmd.trackingmode");
                        metaData.SetAttributeValue(android + "value", "3dof");
                        application.Add(metaData);

                        metaData = new("meta-data");
                        metaData.SetAttributeValue(android + "name", "pico_permission_dim_show");
                        metaData.SetAttributeValue(android + "value", "false");
                        application.Add(metaData);

                        application.SetAttributeValue(android + "screenOrientation", "landscape");
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
                        string app_name = application?.Attribute(android + "label")?.Value;

                        if (isRename || !Regex.IsMatch(app_name, @"@string\/*"))
                            application.SetAttributeValue(android + "label", namePrefix);
                        else
                        {
                            string stringID = app_name.Replace("@string/", string.Empty);

                            foreach (DirectoryInfo dir in new DirectoryInfo(isApkEditor ? ".\\Worker\\res" : ".\\resources\\package_1\\res").GetDirectories())
                            {
                                if (dir.Name.Contains("values"))
                                {
                                    foreach (FileInfo strings in dir.GetFiles("strings.xml"))
                                    {
                                        XDocument stringFile = XDocument.Load(strings.FullName);
                                        XElement stringRoot = stringFile.Root;

                                        foreach (XElement srt in stringRoot.Elements("string"))
                                        {
                                            if (srt.Attribute("name").Value.Contains(stringID))
                                            {
                                                srt.SetValue($"{srt.Value}{namePrefix}");
                                            }
                                        }

                                        stringFile.Save(strings.FullName);
                                    }
                                }
                            }
                        }
                    }

                    xmlFile.Save(".\\Worker\\AndroidManifest.xml");
                }
                catch (Exception e)
                {
                    errorMessage = $"```\n{e.Message}\n```";

                    progressBar.Increase(3);

                    goto skipFile;
                }

                //?? -------------------- [[ Start compiler apk ]] --------------------
                progressBar.Increase();

                if (isApkEditor)
                    errorMessage = await Task.Run(() => Tasks.ApkEditor.Compiler(dirApkUnsing));
                else
                    errorMessage = await Task.Run(() => Tasks.ApkTool.Compiler(dirApkUnsing));

                if (isProcessCancel)
                    goto skipMainTask;
                else if (!string.IsNullOrEmpty(errorMessage))
                {
                    if (isApkEditor)
                    {
                        progressBar.Increase(2);

                        goto skipFile;
                    }
                    else
                    {
                        isApkEditor = true;
                        progressBar.Step += 8;

                        goto startFile;
                    }
                }

                //?? -------------------- [[ Start signing apk ]] --------------------
                progressBar.Increase();

                errorMessage = await Task.Run(() => Tasks.UberApkSigner.Signer(dirApkUnsing, dirApkOut));

                FileInfo dirApkSigned = new($"{dirApkOut.FullName.Replace(dirApkOut.Extension, "")}-aligned-signed.apk");
                FileInfo idsig = new(dirApkSigned + ".idsig");

                dirApkSigned.MoveTo(dirApkOut.FullName);
                if (idsig.Exists)
                    idsig.Delete();

                if (isProcessCancel)
                    goto skipMainTask;
                else if (!string.IsNullOrEmpty(errorMessage))
                {
                    progressBar.Increase(1);
                    goto skipFile;
                }

            skipFile:

                if (string.IsNullOrEmpty(errorMessage))
                {
                    //?? -------------------- [[ File indicator ]] --------------------
                    progressBar.Increase();
                    apkFiles[index] = "✔️ " + file;
                    APKFilesOut[index] = dirApkOut.FullName;
                }
                else
                {
                    if (apkFiles.Count > 1)
                        apkFiles[index] = "✖️ " + file + " 🔘 " + errorMessage;
                    else
                        apkFiles[index] = "✖️ " + file;
                }
            }

            //?? After task
        skipMainTask:
            SoundPlayer simpleSound;

            ChangeStateText($"### Current Status\nCleaning directory...");
            await Task.Run(Utils.DirectoryCleanup);

            if (isProcessCancel)
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

                ChangeStateText($"### Current Status\nAll files have been modified.\n* The APK files are in the Pico folder by the same directory as the original file.\n* Right click file in the box above to see the options.");
                StatusProgressBar.Foreground = new SolidColorBrush(Colors.Green);
                simpleSound = new(@"c:\Windows\Media\Windows Notify Calendar.wav");
            }

            StatusProgressBar.Value = 100;
            isProcessRunning = false;
            IsProcessNotRunning = false;

            ChangeButtonState();

            // Play sound
            simpleSound.Play();
        }

        #region Utils
        private void ChangeButtonState()
        {
            if (APKFiles.Count > 0 && !isProcessRunning)
                StartButton.IsEnabled = true;
            else
                StartButton.IsEnabled = false;

            if (!isProcessCancel && isProcessRunning)
                CancelButton.IsEnabled = true;
            else
                CancelButton.IsEnabled = false;

            if (APKFiles.Count > 0 && !isProcessRunning)
                ClearButton.IsEnabled = true;
            else
                ClearButton.IsEnabled = false;
        }



        private void ResetAppearance()
        {
            ChangeStateText(string.Empty);
            isProcessCancel = false;
            PercentText.Text = string.Empty;
            StatusProgressBar.Value = 0;
            StatusProgressBar.Foreground = ApplicationAccentColorManager.PrimaryAccentBrush;
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