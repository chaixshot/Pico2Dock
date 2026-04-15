using System.Diagnostics;

namespace Pico2Dock
{
    internal class Tasks
    {
        public static Process decompiler;
        public static Process compiler;
        public static Process signer;

        public static string DecompilerTask(string filePath)
        {
            string apkName = System.IO.Path.GetFileName(filePath);

            decompiler = new()
            {
                StartInfo = new ProcessStartInfo
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,

                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,

                    FileName = "java",
                    Arguments = $"-jar \"src/apktool_3.0.1.jar\" d \"{filePath}\" -q -o ./worker",
                }
            };

            try
            {
                decompiler.Start();
                decompiler.WaitForExit();
            }
            catch (Exception)
            {
                return $"ERROR: Unable to run java on machine.\nPlease install Java 17 as recommended.\nhttps://download.oracle.com/java/17/archive/jdk-17.0.12_windows-x64_bin.msi";
            }


            if (decompiler.ExitCode != 0)
                return $"ERROR: file {apkName}\nExit Code: {decompiler.ExitCode}\nLast Output: {decompiler.StandardError.ReadToEnd()}";
            else
                return string.Empty;
        }

        public static string CompilerTask(string apkName)
        {
            compiler = new()
            {
                StartInfo = new ProcessStartInfo
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,

                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,

                    FileName = "java",
                    Arguments = $"-jar \"src/apktool_3.0.1.jar\" b \"./worker\" -q -o \"./singer/{apkName}\"",
                }
            };

            try
            {
                compiler.Start();
                compiler.WaitForExit();
            }
            catch (Exception)
            {
                return $"ERROR: Unable to run java on machine.\nPlease install Java 17 as recommended.\nhttps://download.oracle.com/java/17/archive/jdk-17.0.12_windows-x64_bin.msi";
            }

            if (decompiler.ExitCode != 0)
                return $"ERROR: file {apkName}\nExit Code: {decompiler.ExitCode}\nLast Output: {decompiler.StandardError.ReadToEnd()}";
            else
                return string.Empty;
        }

        public static string SignedTask(string apkName)
        {
            signer = new()
            {
                StartInfo = new ProcessStartInfo
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,

                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,

                    FileName = "java",
                    Arguments = $"-jar \"src/uber-apk-signer-1.3.0.jar\" -a \"./singer/{apkName}\" --ks \"src/testkey.jks\" --ksAlias \"testkey\" --ksKeyPass 114514 --ksPass 114514 --out \"./patched\"",
                }
            };

            try
            {
                signer.Start();
                signer.WaitForExit();
            }
            catch (Exception)
            {
                return $"ERROR: Unable to run java on machine.\nPlease install Java 17 as recommended.\nhttps://download.oracle.com/java/17/archive/jdk-17.0.12_windows-x64_bin.msi";
            }

            if (decompiler.ExitCode != 0)
                return $"ERROR: file {apkName}\nExit Code: {decompiler.ExitCode}\nLast Output: {decompiler.StandardError.ReadToEnd()}";
            else
                return string.Empty;
        }

        public static void KillTask()
        {
            try
            {
                decompiler?.Kill();
                compiler?.Kill();
                signer?.Kill();
            }
            catch (Exception)
            {

            }
        }
    }
}
