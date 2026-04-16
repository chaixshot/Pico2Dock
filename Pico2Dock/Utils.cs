using System.Diagnostics;

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
            string args = string.Format("/e, \"{0}\"", filePath);
            ProcessStartInfo info = new()
            {
                FileName = "explorer",
                Arguments = args
            };
            Process.Start(info);
        }
    }
}
