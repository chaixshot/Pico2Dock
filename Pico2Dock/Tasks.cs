using System.Diagnostics;

namespace Pico2Dock
{
    internal class Tasks
    {
        public static Process? decompiler;
        public static Process? compiler;
        public static Process? signer;

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
                    Arguments = $"-jar \"src/apktool_3.0.1.jar\" decode \"{filePath}\" -q -f -o ./worker",
                }
            };

            decompiler.Start();
            decompiler.WaitForExit();

            if (decompiler.ExitCode != 0)
                return $"### ERROR\n**File:** {apkName}\n**Exit Code:** {decompiler.ExitCode}\n```{decompiler.StandardError.ReadToEnd()}```";
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
                    Arguments = $"-jar \"src/apktool_3.0.1.jar\" build \"./worker\" -q -o \"./singer/{apkName}\"",
                }
            };

            compiler.Start();
            compiler.WaitForExit();

            if (compiler.ExitCode != 0)
                return $"### ERROR\n**File:** {apkName}\n**Exit Code:** {compiler.ExitCode}\n```{compiler.StandardError.ReadToEnd()}```";
            else
                return string.Empty;
        }

        public static string SignedTask(string apkName, string outputDir)
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
                    Arguments = $"-jar \"src/uber-apk-signer-1.3.0.jar\" -a \"./singer/{apkName}\" --ks \"src/keystore.jks\" --ksAlias \"H@mer\" --ksKeyPass forpico2dock --ksPass forpico2dock --out \"{outputDir}\"",
                }
            };

            signer.Start();
            signer.WaitForExit();

            if (signer.ExitCode != 0)
                return $"### ERROR\n**File:** {apkName}\n**Exit Code:** {signer.ExitCode}\n```{signer.StandardError.ReadToEnd()}```";
            else
                return string.Empty;
        }

        public static void KillTasks()
        {
            try
            {
                decompiler?.Kill(true);
                compiler?.Kill(true);
                signer?.Kill(true);
            }
            catch (Exception)
            {

            }
        }
    }
}
