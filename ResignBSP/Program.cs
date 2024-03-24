/*

Copyright (c) 2017-2022, The LumiaWOA & DuoWOA Authors

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

*/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace ResignBSP
{
    internal class Program
    {
        private static void PrintBanner()
        {
            Logging.Log($"ResignBSP {Assembly.GetExecutingAssembly().GetName().Version}");
            Logging.Log("Copyright (c) 2017-2023, The LumiaWOA Authors");
            Logging.Log("https://github.com/WOA-Project/ResignBSP");
            Logging.Log("");
            Logging.Log("This program comes with ABSOLUTELY NO WARRANTY.");
            Logging.Log("This is free software, and you are welcome to redistribute it under certain conditions.");
            Logging.Log("");
        }

        private static void Main(string[] args)
        {
            Console.Title = "ResignBSP";
            PrintBanner();

            try
            {
                if (!File.Exists(Constants.INF2CAT) || !File.Exists(Constants.SignTool))
                {
                    string[] ToolsPaths = KitsHelper.GetInstalledKitToolsPaths();

                    foreach (string ToolsPath in ToolsPaths)
                    {
                        Constants.INF2CAT = Path.Combine(ToolsPath, "x86", "Inf2Cat.exe");
                        Constants.SignTool = Path.Combine(ToolsPath, "x64", "signtool.exe");

                        if (File.Exists(Constants.INF2CAT) && File.Exists(Constants.SignTool))
                        {
                            break;
                        }
                    }
                }

                if (!File.Exists(Constants.INF2CAT) || !File.Exists(Constants.SignTool))
                {
                    throw new Exception("No Windows Kits is installed on the machine");
                }

                if (args.Length < 4)
                {
                    throw new Exception("No arguments specified. Usage: <KMDF Cert> <UMDF Cert> <Cert Password> <Directory> ... <Directory>");
                }

                Constants.kernelModeCertificate = args[0];
                Constants.userModeCertificate = args[1];
                Constants.certificatePassword = args[2];

                if (!File.Exists(Constants.kernelModeCertificate))
                {
                    throw new Exception("No KMDF certificates found on the machine");
                }

                if (!File.Exists(Constants.userModeCertificate))
                {
                    throw new Exception("No UMDF certificates found on the machine");
                }

                string[] Paths = args.Skip(3).Select(Path.GetFullPath).ToArray();

                foreach (string path in Paths)
                {
                    if (!Directory.Exists(path))
                    {
                        if (!File.Exists(path))
                        {
                            throw new Exception($"Path {path} does not exist");
                        }
                        else
                        {
                            switch (Path.GetExtension(path)?.ToLower())
                            {
                                case ".exe":
                                case ".dll":
                                    {
                                        SignFile(path, true);
                                        break;
                                    }
                                case ".sys":
                                    {
                                        SignFile(path);
                                        break;
                                    }
				case ".cat":
				case ".inf":
                                    {
                                        try
                                        {
                                            ProcessDirectory(Path.GetDirectoryName(path));
                                        }
                                        catch { }
                                        break;
                                    }

                                default:
                                    {
                                        throw new Exception($"File {path} is not a valid file");
                                    }
                            }
                        }
                    }
                    else
                    {
                        try
                        {
                            string[] gitModifiedPaths = GitHelper.GetModifiedPathsFromGitRepo(path);
                            foreach (string file in gitModifiedPaths)
                            {
                                try
                                {
                                    switch (Path.GetExtension(file)?.ToLower())
                                    {
                                        case ".exe":
                                        case ".dll":
                                            {
                                                SignFile(file, true);
                                                break;
                                            }
                                        case ".sys":
                                            {
                                                SignFile(file);
                                                break;
                                            }
                                    }
                                }
                                catch { Console.WriteLine($"Failed! {file}"); }
                            }
                        }
                        catch
                        {
                            foreach (string file in Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories))
                            {
                                try
                                {
                                    switch (Path.GetExtension(file)?.ToLower())
                                    {
                                        case ".exe":
                                        case ".dll":
                                            {
                                                SignFile(file, true);
                                                break;
                                            }
                                        case ".sys":
                                            {
                                                SignFile(file);
                                                break;
                                            }
                                    }
                                }
                                catch { Console.WriteLine($"Failed! {file}"); }
                            }
                        }

                        try
                        {
                            string[] gitModifiedPaths = GitHelper.GetModifiedDirectoriesFromGitRepo(path);
                            foreach (string dir in gitModifiedPaths)
                            {
                                try
                                {
                                    ProcessDirectory(dir);
                                }
                                catch { }
                            }
                        }
                        catch
                        {
                            ProcessDirectory(path);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.Log(ex.Message, Logging.LoggingLevel.Error);
                Logging.Log(ex.StackTrace, Logging.LoggingLevel.Error);
            }
        }

        internal static void SignFile(string filePath, bool userModeSigning = false)
        {
            Logging.Log($"Signing: {filePath}");

            Process process = new();
            process.StartInfo.FileName = Constants.SignTool;
            process.StartInfo.Arguments = $@"sign /td sha256 /fd sha256 /f ""{(userModeSigning ? Constants.userModeCertificate : Constants.kernelModeCertificate)}"" /p ""{Constants.certificatePassword}"" /tr http://timestamp.digicert.com ""{filePath}""";
            process.StartInfo.UseShellExecute = false;
            _ = process.Start();
            process.WaitForExit();
        }

        private static void ProcessDirectory(string Directory)
        {
            // 6_3_ARM,10_RS3_ARM64
            // 6_3_ARM
            // 10_RS3_X64
            CleanCatalogs(Directory);
            GenerateCatalogs(Directory, "6_3_ARM,10_RS3_ARM64");
            SignCatalogs(Directory);
        }

        private static void SignCatalogs(string Directory)
        {
            IEnumerable<string> cats = System.IO.Directory.EnumerateFiles(Directory, "*.cat", SearchOption.AllDirectories);

            foreach (string cat in cats)
            {
                if (cat.EndsWith(".cat_"))
                {
                    continue;
                }

                SignFile(cat);
            }
        }

        private static void CleanCatalogs(string Directory)
        {
            List<string> DirectoriesWithINFFiles = GetDirectoriesWithINFFiles(Directory);

            foreach (string dir in DirectoriesWithINFFiles)
            {
                if (System.IO.Directory.EnumerateFiles(dir, "*.cat_").Any())
                {
                    ConsoleColor backup = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Logging.Log("RESOURCE PACKAGE");
                    Console.ForegroundColor = backup;
                }

                IEnumerable<string> cats = System.IO.Directory.EnumerateFiles(dir, "*.cat", SearchOption.AllDirectories);

                foreach (string cat in cats)
                {
                    if (cat.EndsWith(".cat_"))
                    {
                        continue;
                    }

                    File.Delete(cat);
                }
            }
        }

        private static void GenerateCatalogs(string Directory, string OSKey)
        {
            List<string> DirectoriesWithINFFiles = GetDirectoriesWithINFFiles(Directory);

            foreach (string dir in DirectoriesWithINFFiles)
            {
                Logging.Log($"Generating catalog: {dir}");
                Process process = new();
                process.StartInfo.FileName = Constants.INF2CAT;
                process.StartInfo.Arguments = $@"/OS:{OSKey} /Driver:""{dir}""";
                process.StartInfo.UseShellExecute = false;
                _ = process.Start();
                process.WaitForExit();
                process.Dispose();

                if (process.ExitCode != 0 && OSKey.Contains(','))
                {
                    foreach (string SingleOSKey in OSKey.Split(','))
                    {
                        process.Dispose();
                        process = new();
                        process.StartInfo.FileName = Constants.INF2CAT;
                        process.StartInfo.Arguments = $@"/OS:{SingleOSKey} /Driver:""{dir}""";
                        process.StartInfo.UseShellExecute = false;
                        process.Start();
                        process.WaitForExit();
                    }
                }
            }
        }

        private static List<string> GetDirectoriesWithINFFiles(string Directory)
        {
            List<string> lst = [];
            try
            {
                IEnumerable<string> InfFiles = System.IO.Directory.EnumerateFiles(Directory, "*.inf", SearchOption.AllDirectories);
                List<string> DirectoriesWithINFFiles = [];

                foreach (string dir in InfFiles)
                {
                    DirectoriesWithINFFiles.Add(string.Join(@"\", dir.Split('\\').Reverse().Skip(1).Reverse()));
                }

                IOrderedEnumerable<string> DirectoriesWithINFFiles2 = DirectoriesWithINFFiles.Distinct().OrderBy(x => x);

                foreach (string dir in DirectoriesWithINFFiles2)
                {
                    //if (!lst.Any(x => dir.ToLower().StartsWith(x.ToLower())))
                    {
                        lst.Add(dir);
                    }
                }
            }
            catch
            {

            }

            return lst;
        }
    }
}
