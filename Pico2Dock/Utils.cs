using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace Pico2Dock
{
    internal class Utils
    {
        public static string GetAppVersion()
        {
            return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown error reading assembly version";
        }

        public static void OpenExplorer(string filePath)
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

        public static bool IsJavaInstalled()
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

        public static void DirectoryCleanup()
        {
            try
            {
                DirectoryInfo Unsign = new(".\\Unsign");

                if (Unsign.Exists)
                {
                    foreach (FileInfo file in new DirectoryInfo(".\\Unsign").GetFiles())
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
                DirectoryInfo worker = new(".\\Worker");

                if (worker.Exists)
                {
                    foreach (FileInfo file in worker.GetFiles())
                    {
                        file.Delete();
                    }
                    foreach (string dir in Directory.GetDirectories(".\\Worker"))
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
                DirectoryInfo merger = new(".\\Merger");

                if (merger.Exists)
                {
                    foreach (FileInfo file in merger.GetFiles())
                    {
                        file.Delete();
                    }
                    foreach (string dir in Directory.GetDirectories(".\\Merger"))
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
            public double Files = files;
            public double Step = step;

            public void Increase(double mul = 1)
            {
                App.mainWindow.StatusProgressBar.Value += ((100 / Step) * mul) / Files;
                App.mainWindow.PercentText.Text = Math.Floor(App.mainWindow.StatusProgressBar.Value).ToString() + "%";
            }
        }

        public class FileIndicator()
        {
            public static readonly string Working = "🛠️";
            public static readonly string Success = "✔️";
            public static readonly string Error = "✖️";
            public static readonly string ErrorInfo = "🔘";

            public static void ClearAllTag()
            {
                // Remove file indicator except error
                foreach (string filePath in App.mainWindow.APKFiles.ToList())
                {
                    int index = App.mainWindow.APKFiles.IndexOf(filePath);
                    ClearTag(index);
                }
            }

            public static void ClearTag(int index)
            {
                App.mainWindow.APKFiles[index] = Regex.Replace(App.mainWindow.APKFiles[index], $@"({FileIndicator.Working}|{FileIndicator.Success})\s", string.Empty);
            }

            public static string ClearTag(string text)
            {
                return Regex.Replace(text, $@"({FileIndicator.Working}|{FileIndicator.Success})\s", string.Empty);
            }
        }
    }
}
