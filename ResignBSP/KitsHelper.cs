using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ResignBSP
{
    internal class KitsHelper
    {
        public static string[] GetInstalledKitToolsPaths()
        {
            using RegistryKey localMachine = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Default);
            using RegistryKey installedRoots = localMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows Kits\Installed Roots") ?? throw new Exception("No Windows Kits is installed on the machine");

            string KitsRoot10 = (string)installedRoots.GetValue("KitsRoot10") ?? throw new Exception("No Windows Kits is installed on the machine");

            string[] installedVersions = installedRoots.GetSubKeyNames();

            IOrderedEnumerable<string> filteredInstalledVersions = new List<string>(installedVersions)
                .Where(x => x.Count(y => y == '.') == 3)
                .OrderBy(x =>
                {
                    _ = ulong.TryParse(x.Split('.')[2], out ulong BuildNumber);
                    return x;
                });

            return !filteredInstalledVersions.Any()
                ? throw new Exception("No Windows Kits is installed on the machine")
                : filteredInstalledVersions.Select(x => Path.Combine(KitsRoot10, "bin", x)).ToArray();
        }
    }
}
