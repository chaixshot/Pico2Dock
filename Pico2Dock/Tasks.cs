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
                    Arguments = $"-jar \"src\\apktool_3.0.2.jar\" decode \"{filePath}\" --output .\\worker --verbose --force --no-src",
                }
            };
            decompiler.Start();

            while (!decompiler.StandardOutput.EndOfStream)
            {
                string line = decompiler.StandardOutput.ReadLine();
                App.mainWindow.ChangeStateText($"### Current Status\nDecompiling **{apkName}**...\n``{line}``");
            }

            if (decompiler.ExitCode != 0)
                return $"**Exit Code:** {decompiler.ExitCode}\n```\n{decompiler.StandardError.ReadToEnd()}\n```";
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
                    Arguments = $"-jar \"src\\apktool_3.0.2.jar\" build \".\\worker\" --output \".\\singer\\{apkName}\" --verbose",
                }
            };
            compiler.Start();

            while (!compiler.StandardOutput.EndOfStream)
            {
                string line = compiler.StandardOutput.ReadLine();
                App.mainWindow.ChangeStateText($"### Current Status\nCompiling **{apkName}**...\n``{line}``");
            }

            if (compiler.ExitCode != 0)
                return $"**Exit Code:** {compiler.ExitCode}\n```\n{compiler.StandardError.ReadToEnd()}\n```";
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
                    Arguments = $"-jar \"src\\uber-apk-signer-1.3.0.jar\" --apks \".\\singer\\{apkName}\" --ks \"src\\keystore.jks\" --ksAlias \"H@mer\" --ksKeyPass forpico2dock --ksPass forpico2dock --out \"{outputDir}\"",
                }
            };
            signer.Start();

            while (!signer.StandardOutput.EndOfStream)
            {
                string line = signer.StandardOutput.ReadLine();
                App.mainWindow.ChangeStateText($"### Current Status\nSigning **{apkName}**...\n``{line}``");
            }

            if (signer.ExitCode != 0)
                return $"**Exit Code:** {signer.ExitCode}\n```\n {signer.StandardError.ReadToEnd()} \n```";
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
