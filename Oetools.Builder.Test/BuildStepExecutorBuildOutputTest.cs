﻿#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (TaskExecutorWithFileListTest.cs) is part of Oetools.Builder.Test.
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

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Oetools.Builder.History;
using Oetools.Builder.Project;
using Oetools.Builder.Project.Task;
using Oetools.Utilities.Lib;

namespace Oetools.Builder.Test {
    
    [TestClass]
    public class BuildStepExecutorBuildOutputTest {
        
        private static string _testFolder;

        private static string TestFolder => _testFolder ?? (_testFolder = TestHelper.GetTestFolder(nameof(BuildStepExecutorBuildOutputTest)));
                     
        [ClassInitialize]
        public static void Init(TestContext context) {
            Cleanup();
            Utils.CreateDirectoryIfNeeded(TestFolder);
        }


        [ClassCleanup]
        public static void Cleanup() {
            Utils.DeleteDirectoryIfExists(TestFolder, true);
        }

        
        [TestMethod]
        public void BuildStepExecutorBuildOutput_Test_FilesBuilt() {
            
            var outputDir = Path.Combine(TestFolder, "output1");

            Utils.CreateDirectoryIfNeeded(Path.Combine(outputDir, "sourcedir"));
            
            File.WriteAllText(Path.Combine(outputDir, "sourcedir", "file1.ext"), "");
            File.WriteAllText(Path.Combine(outputDir, "sourcedir", "file2.ext"), "");
            File.WriteAllText(Path.Combine(outputDir, "sourcedir", "file3.ext"), "");
            
            var taskExecutor = new BuildStepExecutorBuildOutput {
                Properties = new OeProperties {
                    BuildOptions = new OeBuildOptions {
                        OutputDirectoryPath = outputDir
                    }
                }
            };
            
            var task1 = new TaskOnFile {
                Include = @"**sourcedir**",
                Exclude = "**2.ext",
                TargetFilePath = Path.Combine(outputDir, "newfile"),
                TargetDirectory = "relative"
            };
            
            taskExecutor.Tasks = new List<IOeTask> {
                task1
            };
            
            taskExecutor.Execute();
            
            Assert.AreEqual(2, task1.Files.Count, "only file1.ext and file3 were included");
            
            var taskTargets = task1.Files.SelectMany(f => f.TargetsFiles).ToList();
            
            Assert.AreEqual(4, taskTargets.Count, "we expect 4 targets");
            
            Assert.IsTrue(taskTargets.Exists(t => t.GetTargetPath().Equals(Path.Combine(outputDir, "relative", "file1.ext"))));
            Assert.IsTrue(taskTargets.Exists(t => t.GetTargetPath().Equals(Path.Combine(outputDir, "relative", "file3.ext"))));
            Assert.IsTrue(taskTargets.Exists(t => t.GetTargetPath().Equals(Path.Combine(outputDir, "newfile"))));
            Assert.IsTrue(taskTargets.Exists(t => t.GetTargetPath().Equals(Path.Combine(outputDir, "newfile"))));
        }
        
        private class TaskOnFile : OeTaskFileTargetFile {
            public List<IOeFileToBuildTargetFile> Files { get; set; } = new List<IOeFileToBuildTargetFile>();
            public override void ExecuteForFilesTargetFiles(IEnumerable<IOeFileToBuildTargetFile> files) {
                Files.AddRange(files);
            }
        }
    }
}