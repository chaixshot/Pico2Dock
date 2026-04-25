using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text.RegularExpressions;

namespace Pico2Dock
{
    internal class Tasks
    {
        private static readonly FileInfo dirWorker = new(".\\Worker");
        private static Process? decompiler;
        private static Process? compiler;
        private static Process? merger;
        private static Process? signer;

        internal class ApkTool
        {
            private static readonly FileInfo exec = new(".\\src\\apktool_3.0.2.jar");

            //?? Decompiler
            public static string Decompiler(FileInfo apkFile)
            {

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
                        Arguments = $"-jar {exec} decode \"{apkFile.FullName}\" --output {dirWorker} --force --no-src",
                    }
                };
                decompiler.Start();

                while (!decompiler.StandardOutput.EndOfStream)
                {
                    string line = decompiler.StandardOutput.ReadLine();
                    App.mainWindow.ChangeStateText($"### Current Status\nDecompiling **{apkFile.Name}**...\n``{line}``");
                }

                if (decompiler.ExitCode != 0)
                    return $"**Exit Code:** {decompiler.ExitCode}\n```\n{decompiler.StandardError.ReadToEnd()}\n```";
                else
                    return string.Empty;
            }

            //?? Decompiler
            public static string Compiler(FileInfo apkFile)
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
                        Arguments = $"-jar {exec} build {dirWorker} --output \"{apkFile.FullName}\"",
                    }
                };
                compiler.Start();

                while (!compiler.StandardOutput.EndOfStream)
                {
                    string line = compiler.StandardOutput.ReadLine();
                    App.mainWindow.ChangeStateText($"### Current Status\nCompiling **{apkFile.Name}**...\n``{line}``");
                }

                if (compiler.ExitCode != 0)
                    return $"**Exit Code:** {compiler.ExitCode}\n```\n{compiler.StandardError.ReadToEnd()}\n```";
                else
                    return string.Empty;
            }
        }

        internal class ApkEditor
        {
            private static readonly FileInfo exec = new(".\\src\\APKEditor-1.4.8.jar");

            //?? Decompiler
            public static string Decompiler(FileInfo apkFile)
            {
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
                        Arguments = $"-jar {exec} d -i \"{apkFile.FullName}\" -o {dirWorker} -f -t xml -load-dex 10",
                    }
                };
                decompiler.Start();

                while (!decompiler.StandardOutput.EndOfStream)
                {
                    string line = decompiler.StandardOutput.ReadLine();
                    App.mainWindow.ChangeStateText($"### Current Status\nDecompiling **{apkFile.Name}**...\n``{line}``");
                }

                if (decompiler.ExitCode != 0)
                    return $"**Exit Code:** {decompiler.ExitCode}\n```\n{decompiler.StandardError.ReadToEnd()}\n```";
                else
                    return string.Empty;
            }

            //?? Compiler
            public static string Compiler(FileInfo apkFile)
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
                        Arguments = $"-jar {exec} b -i {dirWorker} -o \"{apkFile.FullName}\" -f -t xml",
                    }
                };
                compiler.Start();

                while (!compiler.StandardOutput.EndOfStream)
                {
                    string line = compiler.StandardOutput.ReadLine();
                    App.mainWindow.ChangeStateText($"### Current Status\nCompiling **{apkFile.Name}**...\n``{line}``");
                }

                if (compiler.ExitCode != 0)
                    return $"**Exit Code:** {compiler.ExitCode}\n```\n{compiler.StandardError.ReadToEnd()}\n```";
                else
                    return string.Empty;
            }

            //?? Merger
            public static string Merger(FileInfo apkFile)
            {
                FileInfo dirMerger = new(".\\Merger");

                // Copy source file to Merger
                apkFile = new FileInfo(apkFile.CopyTo($"{dirMerger}\\{apkFile.Name}").ToString());

                // Remove unnecessary architecture 
                using (ZipArchive zip = ZipFile.Open(apkFile.FullName, ZipArchiveMode.Update))
                {
                    zip.Entries.Where(x => x.FullName.Contains("")).ToList()
                    .ForEach(y =>
                    {
                        string fileName = y.FullName;

                        if (Regex.IsMatch(fileName, "split_config.*.apk")) // APKM
                        {
                            if (!fileName.Contains("arm64_v8a"))
                                zip.GetEntry(fileName).Delete();
                        }
                        else if (Regex.IsMatch(fileName, "config.*.apk")) // XAPK
                        {
                            if (!fileName.Contains("arm64_v8a"))
                                zip.GetEntry(fileName).Delete();
                        }
                    });
                }

                string apkName = apkFile.Name.Replace(".xapk", ".apk").Replace(".apkm", ".apk").Replace(".apks", ".apk");
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
                        Arguments = $"-jar {exec} m -i \"{apkFile.FullName}\" -o {dirMerger}\\{apkName} -f",
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

        internal class UberApkSigner
        {
            private static readonly FileInfo exec = new(".\\src\\uber-apk-signer-1.3.0.jar");

            public static string Signer(FileInfo apkFile, FileInfo outputDir)
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
                        Arguments = $"-jar {exec} --apks \"{apkFile.FullName}\" --ks \"src\\keystore.jks\" --ksAlias \"H@mer\" --ksKeyPass forpico2dock --ksPass forpico2dock --out {outputDir.Directory} --zipAlignPath \"src\\zipalign.exe\"",
                    }
                };
                signer.Start();

                while (!signer.StandardOutput.EndOfStream)
                {
                    string line = signer.StandardOutput.ReadLine();
                    App.mainWindow.ChangeStateText($"### Current Status\nSigning **{apkFile.Name}**...\n``{line}``");
                }

                if (signer.ExitCode != 0)
                    return $"**Exit Code:** {signer.ExitCode}\n```\n {signer.StandardError.ReadToEnd()} \n```";
                else
                    return string.Empty;
            }
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
