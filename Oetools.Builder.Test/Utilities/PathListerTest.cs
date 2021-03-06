﻿#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (SourceFileListerTest.cs) is part of Oetools.Builder.Test.
//
// Oetools.Builder.Test is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// Oetools.Builder.Test is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with Oetools.Builder.Test. If not, see <http://www.gnu.org/licenses/>.
// ========================================================================
#endregion
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DotUtilities;
using DotUtilities.Process;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Oetools.Builder.History;
using Oetools.Builder.Utilities;

namespace Oetools.Builder.Test.Utilities {

    [TestClass]
    public class SourceFileListerTest {

        private static string _testFolder;

        private static string TestFolder => _testFolder ?? (_testFolder = TestHelper.GetTestFolder(Path.Combine(nameof(SourceFileListerTest), Path.GetRandomFileName())));

        [ClassInitialize]
        public static void Init(TestContext context) {
            Cleanup();
            Utils.CreateDirectoryIfNeeded(TestFolder);
        }


        [ClassCleanup]
        public static void Cleanup() {
            Utils.DeleteDirectoryIfExists(TestFolder, true);
        }

        private static GitManager _git = new GitManager();


        [TestMethod]
        public void FilterSourceFiles_DirectoryList_Test() {
            var repoDir = Path.Combine(TestFolder, "dirtests");
            var lister = new PathLister(repoDir);

            Utils.CreateDirectoryIfNeeded(Path.Combine(repoDir, ".git", "sub"));
            Utils.CreateDirectoryIfNeeded(Path.Combine(repoDir, "folder1", "sub"));
            Utils.CreateDirectoryIfNeeded(Path.Combine(repoDir, "folder2"), FileAttributes.Hidden);
            Utils.CreateDirectoryIfNeeded(Path.Combine(repoDir, "folder_special", "sub"));

            Assert.AreEqual(5, lister.GetDirectoryList().Count(), "the default filters should catch .git and .svn");

            lister.FilterOptions = new PathListerFilterOptionsTest {
                Include = @"**sub**"
            };

            Assert.AreEqual(2, lister.GetDirectoryList().Count(), "2 included");

            lister.FilterOptions = new PathListerFilterOptionsTest {
                Include = @"**sub**",
                Exclude = @"**special**"
            };

            Assert.AreEqual(1, lister.GetDirectoryList().Count(), "1 included");


            lister.FilterOptions = new PathListerFilterOptionsTest {
                Exclude = @"**special**"
            };

            Assert.AreEqual(3, lister.GetDirectoryList().Count(), "2");

            lister.FilterOptions = null;

            Assert.AreEqual(5, lister.GetDirectoryList().Count(), "list all but .git");

            lister.FilterOptions = new PathListerFilterOptionsTest {
                ExtraVcsPatternExclusion = ""
            };

            Assert.AreEqual(7, lister.GetDirectoryList().Count(), "list all");

            lister.FilterOptions = new PathListerFilterOptionsTest {
                ExtraVcsPatternExclusion = "",
                OverrideOutputList = Path.Combine(repoDir, "folder1", "sub") + ";" + Path.Combine(repoDir, "folder_special")
            };

            Assert.AreEqual(2, lister.GetDirectoryList().Count(), "use the override list, should list only what is given and exist");

            lister.FilterOptions.OverrideOutputList = null;
            lister.FilterOptions.ExcludeHiddenDirectories = true;

            Assert.AreEqual(6, lister.GetDirectoryList().Count(), "list all but hidden");

            lister.FilterOptions.RecursiveListing = false;

            Assert.AreEqual(3, lister.GetDirectoryList().Count(), "list only top dir");

        }

        [TestMethod]
        public void FilterSourceFiles_Test() {
            var lister = new PathLister("sourcedir");

            var files = new List<OeFile> {
                new OeFile(@"sourcedir\.git\file1"),
                new OeFile(@"sourcedir\.git\subfolder\file2"),
                new OeFile(@"sourcedir\.svn\subfolder\file2"),
                new OeFile(@"sourcedir\legitfile1"),
                new OeFile(@"sourcedir\sub\legitfile2")
            };

            Assert.AreEqual(2, lister.FilterSourceFiles(files).Count(), "the default filters should catch .git and .svn");

            lister.FilterOptions = new PathListerFilterOptionsTest {
                Exclude = @"**\legit*1"
            };

            Assert.AreEqual(1, lister.FilterSourceFiles(files).Count(), "filter file1");

            lister.FilterOptions = new PathListerFilterOptionsTest {
                ExcludeRegex = ".*[fF](ile)?2"
            };


            Assert.AreEqual(1, lister.FilterSourceFiles(files).Count(), "file2");

            lister.FilterOptions = new PathListerFilterOptionsTest {
                Exclude = @"**\legit*1",
                ExcludeRegex = ".*[fF](ile)?2"
            };

            Assert.AreEqual(0, lister.FilterSourceFiles(files).Count(), "all filtered");

            lister.FilterOptions = new PathListerFilterOptionsTest {
                Include = @"**sub**"
            };

            Assert.AreEqual(1, lister.FilterSourceFiles(files).Count(), "1 file included");

            lister.FilterOptions = new PathListerFilterOptionsTest {
                IncludeRegex = @".*",
                ExcludeRegex = ".*[fF](ile)?2"
            };

            Assert.AreEqual(1, lister.FilterSourceFiles(files).Count(), "file1 only");
        }

        [TestMethod]
        public void ClassicListing_Test_File_State_other() {
            var repoDir = Path.Combine(TestFolder, "test_state");

            Utils.CreateDirectoryIfNeeded(Path.Combine(repoDir, "sub"));
            File.WriteAllText(Path.Combine(repoDir, "file1"), "");
            File.WriteAllText(Path.Combine(repoDir, "sub", "file2"), "");
            File.WriteAllText(Path.Combine(repoDir, "sub", "file3"), "");
            File.WriteAllText(Path.Combine(repoDir, "file4"), "");

            var lister = new PathLister(repoDir) {
                OutputOptions = new PathListerOutputOptions {
                    UseLastWriteDateComparison = true,
                    UseCheckSumComparison = false
                }
            };

            var list1 = lister.GetFileList();
            Assert.AreEqual(4, list1.Count, "count all");
            Assert.IsTrue(list1.All(f => f.State == OeFileState.Added), "all added");

            lister.OutputOptions.GetPreviousFileImage = s => list1[s];

            File.Delete(Path.Combine(repoDir, "file4"));
            File.WriteAllText(Path.Combine(repoDir, "sub", "file2"), "content");

            var list2 = lister.GetFileList();
            Assert.AreEqual(3, list2.Count, "count all");
            Assert.AreEqual(1, list2.Count(f => f.State == OeFileState.Modified), "1 modified");
            Assert.AreEqual(2, list2.Count(f => f.State == OeFileState.Unchanged), "2 unchanged");

            lister.OutputOptions.GetPreviousFileImage = s => list2[s];

            File.WriteAllText(Path.Combine(repoDir, "file4"), "");

            var list3 = lister.GetFileList();
            Assert.AreEqual(4,list3.Count, "count all");
            Assert.AreEqual(1,list3.Count(f => f.State == OeFileState.Added), "1 added");
            Assert.AreEqual(3,list3.Count(f => f.State == OeFileState.Unchanged), "3 unchanged");

            lister.OutputOptions.GetPreviousFileImage = s => list3[s];

            File.Delete(Path.Combine(repoDir, "file4"));
            File.Delete(Path.Combine(repoDir, "sub", "file3"));

            var list4 = lister.GetFileList();
            Assert.AreEqual(2, list4.Count, "count all");
            Assert.AreEqual(2, list4.Count(f => f.State == OeFileState.Unchanged), "2 unchanged");

            lister.OutputOptions.GetPreviousFileImage = s => list4[s];

            File.WriteAllText(Path.Combine(repoDir, "file4"), "");

            var list5 = lister.GetFileList();
            Assert.AreEqual(3, list5.Count, "count all");
            Assert.AreEqual(1, list5.Count(f => f.State == OeFileState.Added), "1 added");
            Assert.AreEqual(2, list5.Count(f => f.State == OeFileState.Unchanged), "2 unchanged");
        }

        [TestMethod]
        public void ClassicListing_Test_File_State_modified() {
            var repoDir = Path.Combine(TestFolder, "test_state");
            Utils.CreateDirectoryIfNeeded(Path.Combine(repoDir, "sub"));
            File.WriteAllText(Path.Combine(repoDir, "file1"), "");
            File.WriteAllText(Path.Combine(repoDir, "sub", "file2"), "");
            File.WriteAllText(Path.Combine(repoDir, "sub", "file3"), "");
            File.WriteAllText(Path.Combine(repoDir, "file4"), "");

            var lister = new PathLister(repoDir) {
                OutputOptions = new PathListerOutputOptions {
                    UseLastWriteDateComparison = false,
                    UseCheckSumComparison = false
                }
            };

            var list1 = lister.GetFileList();
            Assert.AreEqual(4, list1.Count, "count all");
            Assert.IsTrue(list1.All(f => f.State == OeFileState.Added), "all added");

            lister.OutputOptions.GetPreviousFileImage = s => list1[s];

            var list2 = lister.GetFileList();
            Assert.IsTrue(list1.All(f => f.State == OeFileState.Added), "make sure we didn't modify previous objects");
            Assert.AreEqual(4, list2.Count, "count all");
            Assert.IsTrue(list2.All(f => f.State == OeFileState.Unchanged), "all unchanged");

            // try modify
            File.WriteAllText(Path.Combine(repoDir, "file1"), "content");

            var list3 = lister.GetFileList();

            Assert.AreEqual(4, list3.Count, "count all");
            Assert.AreEqual(1, list3.Count(f => f.State == OeFileState.Modified), "1 modified");
            Assert.AreEqual(3, list3.Count(f => f.State == OeFileState.Unchanged), "3 unchanged");

            lister.OutputOptions.GetPreviousFileImage = s => list3[s];

            // try modify same size
            File.WriteAllText(Path.Combine(repoDir, "file1"), "conten1");
            File.SetLastWriteTime(Path.Combine(repoDir, "file1"), DateTime.Now.AddHours(1));

            var list4 = lister.GetFileList();

            Assert.AreEqual(4, list4.Count, "count all");
            Assert.IsTrue(list4.All(f => f.State == OeFileState.Unchanged), "all unchanged because the size is the same and that's our only criteria");

            // activate date comparison
            lister.OutputOptions.UseLastWriteDateComparison = true;

            var list5 = lister.GetFileList();

            Assert.AreEqual(4, list5.Count, "count all");
            Assert.AreEqual(1, list5.Count(f => f.State == OeFileState.Modified), "1 modified");
            Assert.AreEqual(3, list5.Count(f => f.State == OeFileState.Unchanged), "3 unchanged");

            lister.OutputOptions.GetPreviousFileImage = s => list5[s];

            var list6 = lister.GetFileList();

            Assert.AreEqual(4, list6.Count, "count all");
            Assert.IsTrue(list6.All(f => f.State == OeFileState.Unchanged), "all unchanged");

            // try hash
            lister.OutputOptions.UseCheckSumComparison = true;

            var list7 = lister.GetFileList();

            Assert.AreEqual(4, list7.Count, "count all");
            Assert.IsTrue(list7.All(f => f.State == OeFileState.Modified), "all modified because the old HASH was null");

            // set up the hash for all files
            list7.ToList().ForEach(f => PathLister.SetFileHash(f));

            lister.OutputOptions.GetPreviousFileImage = s => list7[s];

            var list8 = lister.GetFileList();

            Assert.AreEqual(4, list8.Count, "count all");
            Assert.IsTrue(list8.All(f => f.State == OeFileState.Unchanged), "all unchanged");

            // modify last HASH
            list7.ElementAt(0).Checksum = "fakehash";

            var list9 = lister.GetFileList();

            Assert.AreEqual(4, list9.Count, "count all");
            Assert.AreEqual(1, list9.Count(f => f.State == OeFileState.Modified), "1 modified");
            Assert.AreEqual(3, list9.Count(f => f.State == OeFileState.Unchanged), "3 unchanged");


        }

        [TestMethod]
        public void ClassicListing_Test_filter() {
            var repoDir = Path.Combine(TestFolder, "test_filter");

            Utils.CreateDirectoryIfNeeded(Path.Combine(repoDir, "subfolder"), FileAttributes.Hidden);
            Utils.CreateDirectoryIfNeeded(Path.Combine(repoDir, ".git"));
            Utils.CreateDirectoryIfNeeded(Path.Combine(repoDir, ".svn"));

            File.WriteAllText(Path.Combine(repoDir, "monfichier.txt"), "");
            File.WriteAllText(Path.Combine(repoDir, "subfolder", "monfichier.pls"), "");
            File.WriteAllText(Path.Combine(repoDir, ".git", "testgit.txt"), "");
            File.WriteAllText(Path.Combine(repoDir, ".svn", "testsvn.txt"), "");

            var lister = new PathLister(repoDir) {
                FilterOptions = new PathListerFilterOptionsTest {
                    Exclude = "**((*)).pls"
                }
            };

            Assert.AreEqual(1, lister.GetFileList().Count, "no .pls file");
            Assert.AreEqual(Path.Combine(repoDir, "monfichier.txt"), lister.GetFileList().ElementAt(0).Path, "check file path");

            lister.FilterOptions = null;

            Assert.AreEqual(2, lister.GetFileList().Count, "all files now");

            lister.FilterOptions = new PathListerFilterOptionsTest {
                ExcludeRegex = "\\.[tT][xX][tT]"
            };

            Assert.AreEqual(1, lister.GetFileList().Count, "no .txt file");
            Assert.AreEqual(Path.Combine(repoDir, "subfolder", "monfichier.pls"), lister.GetFileList().ElementAt(0).Path, "check my pls file");

            lister.FilterOptions = new PathListerFilterOptionsTest {
                Include = "**"
            };

            Assert.AreEqual(2, lister.GetFileList().Count, "all files again");

            lister.FilterOptions = new PathListerFilterOptionsTest {
                Include = "**.txt"
            };

            Assert.AreEqual(1, lister.GetFileList().Count, "only include .txt file");

            lister.FilterOptions.ExtraVcsPatternExclusion = "";

            Assert.AreEqual(3, lister.GetFileList().Count, "all .txt even in .git and .svn");

            lister.FilterOptions = new PathListerFilterOptionsTest {
                ExtraVcsPatternExclusion = ""
            };

            Assert.AreEqual(4, lister.GetFileList().Count(), "list all");

            lister.FilterOptions.OverrideOutputList = Path.Combine(repoDir, ".svn", "testsvn.txt") + ";" + Path.Combine(repoDir, "monfichier.txt");

            Assert.AreEqual(2, lister.GetFileList().Count(), "use the override list, should list only what is given and exist");

            lister.FilterOptions.OverrideOutputList = null;
            lister.FilterOptions.ExcludeHiddenDirectories = true;

            Assert.AreEqual(3, lister.GetFileList().Count(), "list all but hidden");

            lister.FilterOptions.ExcludeHiddenDirectories = false;

            Assert.AreEqual(4, lister.GetFileList().Count(), "list all again");

            lister.FilterOptions.RecursiveListing = false;

            Assert.AreEqual(1, lister.GetFileList().Count(), "list only top dir");
        }


        [TestMethod]
        public void Listing_Test_Git_Filter() {

            var repoDir = Path.Combine(TestFolder, "local");
            var lister = new PathLister(repoDir) {
                FilterOptions = null,
                GitFilter = null,
                OutputOptions = new PathListerOutputOptions {
                    UseLastWriteDateComparison = true,
                    UseCheckSumComparison = true
                }
            };

            // set up a local "remote" repo
            _git.SetCurrentDirectory(TestFolder);
            try {
                _git.ExecuteGitCommand(new ProcessArgs().Append("init", "--bare", "remote.git"));
            } catch (Exception) {
                Console.WriteLine("Cancelling test, can't find git!");
                return;
            }

            // clone empty remote
            try {
                _git.ExecuteGitCommand(new ProcessArgs().Append("clone", "remote.git", "local"));
            } catch (Exception e) {
                Assert.IsNotNull(e);
            }

            _git.SetCurrentDirectory(repoDir);
            _git.ExecuteGitCommand(new ProcessArgs().Append(@"config", "user.email", "you@example.com"));
            _git.ExecuteGitCommand(new ProcessArgs().Append(@"config", "user.name", "you"));

            // new branch v1/dev
            try {
                _git.ExecuteGitCommand(new ProcessArgs().Append("checkout", "-b", "v1/dev"));
            } catch (Exception e) {
                Assert.IsNotNull(e);
            }

            Assert.AreEqual(0, lister.GetFileList().Count, "It shouldn't list files in .git folder");

            // add some file
            File.WriteAllText(Path.Combine(repoDir, "init file"), "");

            Assert.AreEqual(1, lister.GetFileList().Count, "now we get one file");
            Assert.AreEqual(Path.Combine(repoDir, "init file"), lister.GetFileList().ElementAt(0).Path, "check that we get what we expect");

            lister.GitFilter = new PathListerGitFilterOptionsTest {
                IncludeSourceFilesCommittedOnlyOnCurrentBranch = true,
                IncludeSourceFilesModifiedSinceLastCommit = true
            };

            Assert.AreEqual(1, lister.GetFileList().Count, "list one file with git commands instead");

            lister.GitFilter.IncludeSourceFilesModifiedSinceLastCommit = false;

            Assert.AreEqual(0, lister.GetFileList().Count, "we don't list files not committed");

            // add files to index
            _git.ExecuteGitCommand(new ProcessArgs().Append("add", "--all"));
            _git.ExecuteGitCommand(new ProcessArgs().Append("commit", "-m", "v1/dev init"));


            Assert.AreEqual(1, lister.GetFileList().Count, "now it's committed so we can list it again");
            Assert.AreEqual(Path.Combine(repoDir, "init file"), lister.GetFileList().ElementAt(0).Path, "check that we get full path");

            // new branch v1/dev/issue1
            try {
                _git.ExecuteGitCommand(new ProcessArgs().Append("checkout", "-b", "v1/ft/issue1"));
            } catch (Exception e) {
                Assert.IsNotNull(e);
            }

            Assert.AreEqual(0, lister.GetFileList().Count, "we are on a new branch so we should not find files committed only into that branch!");

            // add some files
            File.WriteAllText(Path.Combine(repoDir, "new file1"), "");
            Utils.CreateDirectoryIfNeeded(Path.Combine(repoDir, "folder", "subfolder"));
            File.WriteAllText(Path.Combine(repoDir, "folder", "subfolder", "newfile2"), "");

            Assert.AreEqual(0, lister.GetFileList().Count, "we still don't list files not committed");

            lister.GitFilter.IncludeSourceFilesModifiedSinceLastCommit = true;
            lister.GitFilter.IncludeSourceFilesCommittedOnlyOnCurrentBranch = false;

            Assert.AreEqual(2, lister.GetFileList().Count, "but now we do");
            Assert.IsNotNull(lister.GetFileList()[Path.Combine(repoDir, "new file1")], "check that we still get full path");

            // add files to index
            _git.ExecuteGitCommand(new ProcessArgs().Append("add", "--all"));

            Assert.AreEqual(2, lister.GetFileList().Count, "we should also get those 2 files that are on the index but not committed");

            // commit
            _git.ExecuteGitCommand(new ProcessArgs().Append("commit", "-m", "v1/dev/issue fixing"));


            Assert.AreEqual(0, lister.GetFileList().Count, "now we committed so we don't see them anymore...");

            lister.GitFilter.IncludeSourceFilesCommittedOnlyOnCurrentBranch = true;

            Assert.AreEqual(2, lister.GetFileList().Count, "but now they are committed so we should see them with the other filter");

            // add some files
            File.WriteAllText(Path.Combine(repoDir, "newfile3"), "");
            Utils.CreateDirectoryIfNeeded(Path.Combine(repoDir, "cool"));
            File.WriteAllText(Path.Combine(repoDir, "cool", "newfile4"), "");

            // add files to index
            _git.ExecuteGitCommand(new ProcessArgs().Append("add", "--all"));
            _git.ExecuteGitCommand(new ProcessArgs().Append("commit", "-m", "v1/dev/issue still fixing"));

            // add some file
            File.WriteAllText(Path.Combine(repoDir, "newfile5"), "");

            Assert.AreEqual(5, lister.GetFileList().Count, "with both filter, we should see 5 files");

            lister.FilterOptions = new PathListerFilterOptionsTest {
                Exclude = "**file5"
            };

            Assert.AreEqual(4, lister.GetFileList().Count, "we applied a source path filter");

            lister.FilterOptions = new PathListerFilterOptionsTest {
                Include = "**file5"
            };

            Assert.AreEqual(1, lister.GetFileList().Count, "we applied an include source path filter");

            lister.FilterOptions = null;

            // push branch
            try {
                _git.ExecuteGitCommand(new ProcessArgs().Append("push", "-u", "origin", "v1/ft/issue1"));
            } catch (Exception e) {
                Assert.IsNotNull(e);
            }

            Assert.AreEqual(5, lister.GetFileList().Count, "we should still see 5 files because the remote branch of the current branch doesn't count as merge");

            // commit
            _git.ExecuteGitCommand(new ProcessArgs().Append("add", "--all"));
            _git.ExecuteGitCommand(new ProcessArgs().Append("commit", "-m", "v1/dev/issue fixing again"));

            // merge feat onto dev
            try {
                _git.ExecuteGitCommand(new ProcessArgs().Append("checkout", "v1/dev"));
            } catch (Exception e) {
                Assert.IsNotNull(e);
            }
            _git.ExecuteGitCommand(new ProcessArgs().Append("merge", "v1/ft/issue1"));
            try {
                _git.ExecuteGitCommand(new ProcessArgs().Append("checkout", "v1/ft/issue1"));
            } catch (Exception e) {
                Assert.IsNotNull(e);
            }

            Assert.AreEqual(0, lister.GetFileList().Count, "now we should have 0 files because the last commit is also the last merge commit and we have nothing in our working copy");

            // add some file
            File.WriteAllText(Path.Combine(repoDir, "newfile6"), "");
            // add files to index
            _git.ExecuteGitCommand(new ProcessArgs().Append("add", "--all"));
            _git.ExecuteGitCommand(new ProcessArgs().Append("commit", "-m", "v1/dev/issue still fixing"));

            // push branch
            try {
                _git.ExecuteGitCommand(new ProcessArgs().Append("push", "-u", "origin", "v1/ft/issue1"));
            } catch (Exception e) {
                Assert.IsNotNull(e);
            }

            // checkout tag
            try {
                _git.ExecuteGitCommand(new ProcessArgs().Append("checkout", "origin/v1/ft/issue1"));
            } catch (Exception e) {
                Assert.IsNotNull(e);
            }

            // delete branch v1/ft/issue1
            try {
                _git.ExecuteGitCommand(new ProcessArgs().Append("branch", "-D", "v1/ft/issue1"));
            } catch (Exception e) {
                Assert.IsNotNull(e);
            }

            lister.GitFilter.IncludeSourceFilesModifiedSinceLastCommit = false;

            Assert.AreEqual(1, lister.GetFileList().Count, "we are in detached mode, but on a commit that reference origin/v1/ft/issue1, so we presume we are on this branch and return the list of files modified from this branch to the last merge (which is 1 commit ago on v1/dev)");

            lister.GitFilter.CurrentBranchName = "v1/ft/issue1";

            Assert.AreEqual(1, lister.GetFileList().Count, "Same thing, but this time we pointed the right branch");

            lister.GitFilter.CurrentBranchOriginCommit = "HEAD";

            Assert.AreEqual(0, lister.GetFileList().Count, "Now we explicitly tell which commit is the first of the branch");

        }

        private class PathListerGitFilterOptionsTest : PathListerGitFilterOptions {}

        private class PathListerFilterOptionsTest : PathListerFilterOptions {}
    }
}
