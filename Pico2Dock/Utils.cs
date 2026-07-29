using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace Pico2Dock
{
    internal class Utils
    {
        internal static DirectoryInfo TempFolder = new(Path.Combine(Path.GetTempPath(), "Pico2Dock"));

        internal static string GetAppVersion()
        {
            return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown error reading assembly version";
        }

        internal static void OpenExplorer(string filePath)
        {
            string args;
            if (System.IO.Path.GetExtension(filePath) != string.Empty)
                args = string.Format("/e ,/select, \"{0}\"", filePath);
            else
                args = string.Format("/e, \"{0}\"", filePath);
            ProcessStartInfo info = new()
            {
                FileName = "explorer",
                Arguments = args
            };
            Process.Start(info);
        }

        internal static bool IsJavaInstalled()
        {
            Process java = new()
            {
                StartInfo = new ProcessStartInfo
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,

                    FileName = "java",
                    Arguments = $"-version",
                }
            };

            try
            {
                java.Start();
                java.WaitForExit();

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        internal static void DirectoryCleanup()
        {
            try
            {
                DirectoryInfo Unsign = new($"{TempFolder}\\Unsign");

                if (Unsign.Exists)
                {
                    foreach (FileInfo file in new DirectoryInfo($"{TempFolder}\\Unsign").GetFiles())
                    {
                        file.Delete();
                    }
                }
            }
            catch (Exception ex)
            {
                App.mainWindow.ChangeStateText($"```\n{ex}\n```");
            }

            try
            {
                DirectoryInfo worker = new($"{TempFolder}\\Worker");

                if (worker.Exists)
                {
                    foreach (FileInfo file in worker.GetFiles())
                    {
                        file.Delete();
                    }
                    foreach (string dir in Directory.GetDirectories($"{TempFolder}\\Worker"))
                    {
                        Directory.Delete(dir, true);
                    }
                }
            }
            catch (Exception ex)
            {
                App.mainWindow.ChangeStateText($"```\n{ex}\n```");
            }

            try
            {
                DirectoryInfo merger = new($"{TempFolder}\\Merger");

                if (merger.Exists)
                {
                    foreach (FileInfo file in merger.GetFiles())
                    {
                        file.Delete();
                    }
                    foreach (string dir in Directory.GetDirectories($"{TempFolder}\\Merger"))
                    {
                        Directory.Delete(dir, true);
                    }
                }
            }
            catch (Exception ex)
            {
                App.mainWindow.ChangeStateText($"```\n{ex}\n```");
            }
        }

        internal class ProgressBar(double files, double step)
        {
            internal double Files = files;
            internal double Step = step;

            internal void Increase(double mul = 1)
            {
                App.mainWindow.StatusProgressBar.Value += ((100 / Step) * mul) / Files;
                App.mainWindow.PercentText.Text = Math.Floor(App.mainWindow.StatusProgressBar.Value).ToString() + "%";
            }
        }

        internal class FileIndicator()
        {
            internal static readonly string Working = "🛠️";
            internal static readonly string Success = "✔️";
            internal static readonly string Error = "✖️";
            internal static readonly string ErrorInfo = "🔘";

            internal static void ClearAllTag()
            {
                // Remove file indicator except error
                foreach (string filePath in App.mainWindow.APKFiles.ToList())
                {
                    int index = App.mainWindow.APKFiles.IndexOf(filePath);
                    ClearTag(index);
                }
            }

            internal static void ClearTag(int index)
            {
                App.mainWindow.APKFiles[index] = Regex.Replace(App.mainWindow.APKFiles[index], $@"({FileIndicator.Working}|{FileIndicator.Success})\s", string.Empty);
            }

            internal static string ClearTag(string text)
            {
                return Regex.Replace(text, $@"({FileIndicator.Working}|{FileIndicator.Success})\s", string.Empty);
            }
        }
    }
}
