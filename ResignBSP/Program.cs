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
﻿using System;
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
                if (!File.Exists(Constants.inf2cat) || !File.Exists(Constants.signtool))
                {
                    string[] ToolsPaths = KitsHelper.GetInstalledKitToolsPaths();

                    foreach (string ToolsPath in ToolsPaths)
                    {
                        Constants.inf2cat = Path.Combine(ToolsPath, "x86", "Inf2Cat.exe");
                        Constants.signtool = Path.Combine(ToolsPath, "x64", "signtool.exe");

                        if (File.Exists(Constants.inf2cat) && File.Exists(Constants.signtool))
                        {
                            break;
                        }
                    }
                }

                if (!File.Exists(Constants.inf2cat) || !File.Exists(Constants.signtool))
                {
                    throw new Exception("No Windows Kits is installed on the machine");
                }

                if (args.Length <= 0)
                {
                    throw new Exception("No arguments specified");
                }

                string[] Directories = args;

                if (args[0].EndsWith(".pfx") && args.Length > 2)
                {
                    Directories = args.Skip(2).ToArray();
                    Constants.cert = args[0];
                    Constants.certpassword = args[1];
                }

                if (!File.Exists(Constants.cert))
                {
                    throw new Exception("No certificates found on the machine");
                }

                foreach (var directory in Directories)
                {
                    try
                    {
                        string[] gitModifiedPaths = GitHelper.GetModifiedDirectoriesFromGitRepo(directory);
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
                        ProcessDirectory(directory);
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.Log(ex.Message, Logging.LoggingLevel.Error);
                Logging.Log(ex.StackTrace, Logging.LoggingLevel.Error);
            }
        }

        internal static void SignFile(string filePath, bool usermodesigning = false)
        {
            Process process = new();
            process.StartInfo.FileName = signtool;
            process.StartInfo.Arguments = "sign /td sha256 /fd sha256 " + <REDACTED FOR SOURCE CODE COMPLIANCE> + " /tr http://timestamp.digicert.com \"" + filePath + "\"";
            process.StartInfo.UseShellExecute = false;
            _ = process.Start();
            process.WaitForExit();
        }

        private static void ProcessDirectory(string Directory)
        {
            //GenerateCatalog(Directory, "6_3_ARM,10_RS3_ARM64");
            //GenerateCatalog(Directory, "6_3_ARM");
            GenerateCatalogs(Directory, "10_RS3_ARM64");
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

                Logging.Log("Signing: " + cat);
                SignFile(cat);
            }
        }

        private static void GenerateCatalogs(string Directory, string OSKey)
        {
            List<string> DirsWithInfs = GetDirectoriesWithInfs(Directory);

            foreach (string dir in DirsWithInfs)
            {
                if (System.IO.Directory.EnumerateFiles(dir, "*.cat_").Any())
                {
                    ConsoleColor backup = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Logging.Log("RESOURCE PACKAGE");
                    Console.ForegroundColor = backup;
                }

                Logging.Log("Generating catalog: " + dir);
                Process process = new();
                process.StartInfo.FileName = Constants.inf2cat;
                process.StartInfo.Arguments = $"/OS:{OSKey} /Driver:\"{dir}\"";
                process.StartInfo.UseShellExecute = false;
                _ = process.Start();
                process.WaitForExit();
                process.Dispose();
            }
        }

        private static List<string> GetDirectoriesWithInfs(string Directory)
        {
            List<string> lst = new();
            try
            {
                IEnumerable<string> InfFiles = System.IO.Directory.EnumerateFiles(Directory, "*.inf", SearchOption.AllDirectories);
                List<string> DirsWithInfs = new();

                foreach (string dir in InfFiles)
                {
                    DirsWithInfs.Add(string.Join("\\", dir.Split('\\').Reverse().Skip(1).Reverse()));
                }

                IOrderedEnumerable<string> DirsWithInfs2 = DirsWithInfs.Distinct().OrderBy(x => x);

                foreach (string dir in DirsWithInfs2)
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
