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
    }
}
