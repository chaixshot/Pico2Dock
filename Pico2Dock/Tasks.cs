using System.Diagnostics;
using System.IO;

namespace Pico2Dock
{
    internal class Tasks
    {
        public static Process? decompiler;
        public static Process? compiler;
        public static Process? merger;
        public static Process? signer;

        internal class ApkTool
        {
            //?? Decompiler
            public static string Decompiler(string filePath)
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

            //?? Decompiler
            public static string Compiler(string apkName)
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
        }

        internal class ApkEditor
        {
            //?? Decompiler
            public static string Decompiler(string filePath)
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
                        Arguments = $"-jar \"src\\APKEditor-1.4.8.jar\" d -i \"{filePath}\" -o .\\worker -f -t xml -res-dir \"resssss/*\" -load-dex 10",
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

            //?? Compiler
            public static string Compiler(string apkName)
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
                        Arguments = $"-jar \"src\\APKEditor-1.4.8.jar\" b -i \".\\worker\" -o \".\\singer\\{apkName}\" -f -t xml -res-dir \".\\res\"",
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

            //?? Merger
            public static string Merger(string filePath)
            {
                string apkName = Path.GetFileName(filePath).Replace(".xapk", ".apk").Replace(".apkm", ".apk").Replace(".apks", ".apk");

                merger = new()
                {
                    StartInfo = new ProcessStartInfo
                    {
                        CreateNoWindow = true,
                        UseShellExecute = false,

                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        RedirectStandardInput = true,

                        FileName = "java",
                        Arguments = $"-jar \"src\\APKEditor-1.4.8.jar\" m -i \"{filePath}\" -o \".\\merger\\{apkName}\" -f",
                    }
                };
                merger.Start();

                while (!merger.StandardOutput.EndOfStream)
                {
                    string line = merger.StandardOutput.ReadLine();
                    App.mainWindow.ChangeStateText($"### Current Status\nMerging **{apkName}**...\n``{line}``");
                }

                if (merger.ExitCode != 0)
                    return $"**Exit Code:** {merger.ExitCode}\n```\n{merger.StandardError.ReadToEnd()}\n```";
                else
                    return string.Empty;
            }
        }

        //??
        public static string Signed(string apkName, string outputDir)
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
                    Arguments = $"-jar \"src\\uber-apk-signer-1.3.0.jar\" --apks \".\\singer\\{apkName}\" --ks \"src\\keystore.jks\" --ksAlias \"H@mer\" --ksKeyPass forpico2dock --ksPass forpico2dock --out \"{outputDir}\" --zipAlignPath \"src\\zipalign.exe\"",
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

        //??
        public static void KillTasks()
        {
            try
            {
                decompiler?.Kill(true);
                compiler?.Kill(true);
                merger?.Kill(true);
                signer?.Kill(true);
            }
            catch (Exception)
            {

            }
        }
    }
}
