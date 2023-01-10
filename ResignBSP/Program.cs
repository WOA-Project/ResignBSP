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
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace ResignBSP
{
    class Program
    {
        static string inf2cat = @"C:\Program Files (x86)\Windows Kits\10\bin\x86\Inf2Cat.exe";
        static string signtool = @"C:\Program Files (x86)\Windows Kits\10\bin\10.0.22000.0\x64\signtool.exe";

        static void Main(string[] args)
        {
            Console.Title = "ResignBSP";

            Logging.Log($"ResignBSP {Assembly.GetExecutingAssembly().GetName().Version}");
            Logging.Log("Copyright (c) 2017-2022, The LumiaWOA & DuoWOA Authors");
            Logging.Log("https://github.com/WOA-Project/ResignBSP");
            Logging.Log("");
            Logging.Log("This program comes with ABSOLUTELY NO WARRANTY.");
            Logging.Log("This is free software, and you are welcome to redistribute it under certain conditions.");
            Logging.Log("");

            try
            {
                string[] ToolsPaths = GetInstalledKitToolsPaths();

                foreach (string ToolsPath in ToolsPaths)
                {
                    inf2cat = Path.Combine(ToolsPath, "x86", "Inf2Cat.exe");
                    signtool = Path.Combine(ToolsPath, "x64", "signtool.exe");

                    if (File.Exists(inf2cat) && File.Exists(signtool))
                    {
                        break;
                    }
                }

                if (!File.Exists(inf2cat) || !File.Exists(signtool))
                {
                    throw new Exception("No Windows Kits is installed on the machine");
                }

                if (args.Count() <= 0)
                {
                    throw new Exception("No arguments specified");
                }

                string[] Directories = args;

                foreach (var dir in Directories)
                {
                    ProcessDirectory(dir);
                }
            }
            catch (Exception ex)
            {
                Logging.Log(ex.Message, Logging.LoggingLevel.Error);
                Logging.Log(ex.StackTrace, Logging.LoggingLevel.Error);
            }
        }

        static void SignFile(string filePath)
        {
            Process process = new();
            process.StartInfo.FileName = signtool;
            process.StartInfo.Arguments = "sign /td sha256 /fd sha256 " + <REDACTED FOR SOURCE CODE COMPLIANCE> + " /tr http://timestamp.digicert.com \"" + filePath + "\"";
            process.StartInfo.UseShellExecute = false;
            process.Start();
            process.WaitForExit();
        }

        static void ProcessDirectory(string Directory)
        {

            var DirsWithInfs = GetDirectoriesWithInfs(Directory);

            foreach (var dir in DirsWithInfs)
            {
                if (System.IO.Directory.EnumerateFiles(dir, "*.cat_").Count() > 0)
                {
                    var backup = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Logging.Log("RESOURCE PACKAGE");
                    Console.ForegroundColor = backup;
                }

                Logging.Log("Generating catalog: " + dir);
                Process process = new();
                process.StartInfo.FileName = inf2cat;
                process.StartInfo.Arguments = "/OS:6_3_ARM,10_RS3_ARM64 /Driver:\"" + dir + "\"";
                process.StartInfo.UseShellExecute = false;
                process.Start();
                process.WaitForExit();
                if (process.ExitCode != 0)
                {
                    process.Dispose();
                    process = new();
                    process.StartInfo.FileName = inf2cat;
                    process.StartInfo.Arguments = "/OS:6_3_ARM /Driver:\"" + dir + "\"";
                    process.StartInfo.UseShellExecute = false;
                    process.Start();
                    process.WaitForExit();

                    process.Dispose();
                    process = new();
                    process.StartInfo.FileName = inf2cat;
                    process.StartInfo.Arguments = "/OS:10_RS3_ARM64 /Driver:\"" + dir + "\"";
                    process.StartInfo.UseShellExecute = false;
                    process.Start();
                    process.WaitForExit();
                }
            }

            var cats = System.IO.Directory.EnumerateFiles(Directory, "*.cat", System.IO.SearchOption.AllDirectories);

            foreach (var cat in cats)
            {
                if (cat.EndsWith(".cat_"))
                    continue;
                Logging.Log("Signing: " + cat);
                SignFile(cat);
            }
        }

        static List<string> GetFilesToSign(string Directory)
        {
            var SysFiles = System.IO.Directory.EnumerateFiles(Directory, "*.sys", System.IO.SearchOption.AllDirectories);
            var DllFiles = System.IO.Directory.EnumerateFiles(Directory, "*.dll", System.IO.SearchOption.AllDirectories);
            var ExeFiles = System.IO.Directory.EnumerateFiles(Directory, "*.exe", System.IO.SearchOption.AllDirectories);

            List<string> files = new List<string>();
            files.AddRange(SysFiles);
            files.AddRange(DllFiles);
            files.AddRange(ExeFiles);

            return files;
        }

        static List<string> GetDirectoriesWithInfs_(string Directory)
        {
            var InfFiles = System.IO.Directory.EnumerateFiles(Directory, "*.inf_", System.IO.SearchOption.AllDirectories);
            var DirsWithInfs = new List<string>();

            foreach (var dir in InfFiles)
                DirsWithInfs.Add(string.Join("\\", dir.Split('\\').Reverse().Skip(1).Reverse()));

            var DirsWithInfs2 = DirsWithInfs.Distinct().OrderBy(x => x);
            var lst = new List<string>();

            foreach (var dir in DirsWithInfs2)
            {
                if (!lst.Any(x => dir.ToLower().StartsWith(x.ToLower())))
                {
                    lst.Add(dir);
                }
            }

            return lst;
        }

        static List<string> GetDirectoriesWithInfs(string Directory)
        {
            var InfFiles = System.IO.Directory.EnumerateFiles(Directory, "*.inf", System.IO.SearchOption.AllDirectories);
            var DirsWithInfs = new List<string>();

            foreach (var dir in InfFiles)
                DirsWithInfs.Add(string.Join("\\", dir.Split('\\').Reverse().Skip(1).Reverse()));

            var DirsWithInfs2 = DirsWithInfs.Distinct().OrderBy(x => x);
            var lst = new List<string>();

            foreach (var dir in DirsWithInfs2)
            {
                //if (!lst.Any(x => dir.ToLower().StartsWith(x.ToLower())))
                {
                    lst.Add(dir);
                }
            }

            return lst;
        }

        public static string[] GetInstalledKitToolsPaths()
        {
            using (RegistryKey localMachine = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Default))
            {
                using (RegistryKey installedRoots = localMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows Kits\Installed Roots"))
                {
                    if (installedRoots == null)
                    {
                        throw new Exception("No Windows Kits is installed on the machine");
                    }

                    string KitsRoot10 = (string)installedRoots.GetValue("KitsRoot10");

                    if (KitsRoot10 == null)
                    {
                        throw new Exception("No Windows Kits is installed on the machine");
                    }

                    string[] installedVersions = installedRoots.GetSubKeyNames();

                    IOrderedEnumerable<string> filteredInstalledVersions = (new List<string>(installedVersions))
                        .Where(x => x.Count(y => y == '.') == 3)
                        .OrderBy(x =>
                        {
                            ulong BuildNumber = 0;
                            ulong.TryParse(x.Split('.')[2], out BuildNumber);
                            return x;
                        });

                    if (filteredInstalledVersions.Count() <= 0)
                    {
                        throw new Exception("No Windows Kits is installed on the machine");
                    }

                    return filteredInstalledVersions.Select(x => Path.Combine(KitsRoot10, "bin", x)).ToArray();
                }
            }
        }
    }
}
