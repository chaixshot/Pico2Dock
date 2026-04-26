using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text.RegularExpressions;

namespace Pico2Dock
{
    internal class Tasks
    {
        private static readonly FileInfo dirWorker = new(".\\Worker");
        private static readonly MainWindow mainWindow = App.mainWindow;
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
                mainWindow.ChangeStateText($"### Decoder\nDecompiling resources of **{apkFile.Name}**...");

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
                        Arguments = $"-jar {exec} decode \"{apkFile.FullName}\" --output \"{dirWorker}\" --force --no-src",
                    }
                };
                decompiler.Start();

                while (!decompiler.StandardOutput.EndOfStream)
                {
                    string line = decompiler.StandardOutput.ReadLine();
                    mainWindow.ChangeStateText($"### Decoder\nDecompiling resources of **{apkFile.Name}**...\n``{line}``");
                }

                if (decompiler.ExitCode != 0)
                    return $"**Exit Code:** {decompiler.ExitCode}\n```\n{decompiler.StandardError.ReadToEnd()}\n```";
                else
                    return string.Empty;
            }

            //?? Decompiler
            public static string Compiler(FileInfo apkFile)
            {
                mainWindow.ChangeStateText($"### Encoder\nBuilding **{apkFile.Name}**...");

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
                        Arguments = $"-jar {exec} build \"{dirWorker}\" --output \"{apkFile.FullName}\"",
                    }
                };
                compiler.Start();

                while (!compiler.StandardOutput.EndOfStream)
                {
                    string line = compiler.StandardOutput.ReadLine();
                    mainWindow.ChangeStateText($"### Encoder\nBuilding **{apkFile.Name}**...\n``{line}``");
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
                mainWindow.ChangeStateText($"### Decoder\nDecompiling resources of **{apkFile.Name}**...");

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
                        Arguments = $"-jar {exec} d -i \"{apkFile.FullName}\" -o \"{dirWorker}\" -f -t xml -load-dex 10",
                    }
                };
                decompiler.Start();

                while (!decompiler.StandardError.EndOfStream)
                {
                    string line = decompiler.StandardError.ReadLine();
                    mainWindow.ChangeStateText($"### Decoder\nDecompiling resources of **{apkFile.Name}**...\n``{line}``");
                }

                if (decompiler.ExitCode != 0)
                    return $"**Exit Code:** {decompiler.ExitCode}\n```\n{decompiler.StandardError.ReadToEnd()}\n```";
                else
                    return string.Empty;
            }

            //?? Compiler
            public static string Compiler(FileInfo apkFile)
            {
                mainWindow.ChangeStateText($"### Encoder\nBuilding **{apkFile.Name}**...");

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
                        Arguments = $"-jar {exec} b -i \"{dirWorker}\" -o \"{apkFile.FullName}\" -f -t xml",
                    }
                };
                compiler.Start();

                while (!compiler.StandardError.EndOfStream)
                {
                    string line = compiler.StandardError.ReadLine();
                    mainWindow.ChangeStateText($"### Encoder\nBuilding **{apkFile.Name}**...\n``{line}``");
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
                mainWindow.ChangeStateText($"### Merger\n**{apkFile.Name}**\nRemoving unnecessary architecture...");
                using (ZipArchive zip = ZipFile.Open(apkFile.FullName, ZipArchiveMode.Update))
                {
                    bool pickArm64v8a = false;

                    foreach (var item in zip.Entries.ToList())
                    {
                        string fileName = item.FullName;

                        if (Regex.IsMatch(fileName, @"\w*config.[\w]{3,}.apk")) // is architecture file
                        {
                            if (Regex.IsMatch(fileName, ".*arm64_v8a.*")) // is arm64_v8a
                                pickArm64v8a = true;
                            else
                            {
                                if (Regex.IsMatch(fileName, ".*armeabi_v7a.*")) // is armeabi_v7a
                                {
                                    if (pickArm64v8a) // is no arm64_v8a
                                        zip.GetEntry(fileName).Delete();
                                }
                                else
                                    zip.GetEntry(fileName).Delete();
                            }
                        }
                    }
                }

                string apkName = apkFile.Name.Replace(apkFile.Extension, ".apk");
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
                        Arguments = $"-jar {exec} m -i \"{apkFile.FullName}\" -o \"{dirMerger}\\{apkName}\" -f",
                    }
                };
                merger.Start();
                mainWindow.ChangeStateText($"### Merger\nMerging multiple splitted **{apkFile.Name}**...");

                while (!merger.StandardError.EndOfStream)
                {
                    string line = merger.StandardError.ReadLine();
                    mainWindow.ChangeStateText($"### Merger\nMerging multiple splitted **{apkFile.Name}**...\n``{line}``");
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
                mainWindow.ChangeStateText($"### Signer\nSigning **{apkFile.Name}**`");

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
                        Arguments = $"-jar {exec} --apks \"{apkFile.FullName}\" --ks \".\\src\\keystore.jks\" --ksAlias \"H@mer\" --ksKeyPass forpico2dock --ksPass forpico2dock --out \"{outputDir.Directory}\" --zipAlignPath \".\\src\\zipalign.exe\"",
                    }
                };
                signer.Start();

                while (!signer.StandardOutput.EndOfStream)
                {
                    string line = signer.StandardOutput.ReadLine();
                    mainWindow.ChangeStateText($"### Signer\nSigning **{apkFile.Name}**...\n``{line}``");
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
