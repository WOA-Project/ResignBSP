using LibGit2Sharp;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ResignBSP
{
    internal class GitHelper
    {
        internal static string[] GetModifiedDirectoriesFromGitRepo(string gitRepoPath)
        {
            HashSet<string> modifiedPaths = new();

            using Repository repository = new(gitRepoPath);

            RepositoryStatus status = repository.RetrieveStatus(new StatusOptions()
            {
                IncludeUntracked = true
            });

            IEnumerable<string> added = status.Added.Select(x => x.FilePath);
            IEnumerable<string> missing = status.Missing.Select(x => x.FilePath);
            IEnumerable<string> modified = status.Modified.Select(x => x.FilePath);
            IEnumerable<string> removed = status.Removed.Select(x => x.FilePath);
            IEnumerable<string> renamedinindex = status.RenamedInIndex.SelectMany(x => new string[] { x.FilePath, x.HeadToIndexRenameDetails.OldFilePath });
            IEnumerable<string> renamedinworkdir = status.RenamedInWorkDir.SelectMany(x => new string[] { x.FilePath, x.IndexToWorkDirRenameDetails.OldFilePath });
            IEnumerable<string> staged = status.Staged.Select(x => x.FilePath);
            IEnumerable<string> untracked = status.Untracked.Select(x => x.FilePath);

            foreach (string element in added)
            {
                if (string.IsNullOrEmpty(element))
                {
                    continue;
                }

                _ = modifiedPaths.Add(element);
            }

            foreach (string element in missing)
            {
                if (string.IsNullOrEmpty(element))
                {
                    continue;
                }

                _ = modifiedPaths.Add(element);
            }

            foreach (string element in modified)
            {
                if (string.IsNullOrEmpty(element))
                {
                    continue;
                }

                _ = modifiedPaths.Add(element);
            }

            foreach (string element in removed)
            {
                if (string.IsNullOrEmpty(element))
                {
                    continue;
                }

                _ = modifiedPaths.Add(element);
            }

            foreach (string element in renamedinindex)
            {
                if (string.IsNullOrEmpty(element))
                {
                    continue;
                }

                _ = modifiedPaths.Add(element);
            }

            foreach (string element in renamedinworkdir)
            {
                if (string.IsNullOrEmpty(element))
                {
                    continue;
                }

                _ = modifiedPaths.Add(element);
            }

            foreach (string element in staged)
            {
                if (string.IsNullOrEmpty(element))
                {
                    continue;
                }

                _ = modifiedPaths.Add(element);
            }

            foreach (string element in untracked)
            {
                if (string.IsNullOrEmpty(element))
                {
                    continue;
                }

                _ = modifiedPaths.Add(element);
            }

            string[] modifiedDirectories = modifiedPaths.Select(x => Path.Combine(gitRepoPath, Path.GetDirectoryName(x))).Distinct().ToArray();
            return modifiedDirectories;
        }
    }
}
