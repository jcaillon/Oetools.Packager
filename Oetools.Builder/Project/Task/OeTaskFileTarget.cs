﻿#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (OeTaskFileTarget.cs) is part of Oetools.Builder.
// 
// Oetools.Builder is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Oetools.Builder is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Oetools.Builder. If not, see <http://www.gnu.org/licenses/>.
// ========================================================================
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Oetools.Builder.Exceptions;
using Oetools.Builder.History;
using Oetools.Builder.Utilities;
using Oetools.Utilities.Lib;
using Oetools.Utilities.Lib.Extension;
using Oetools.Utilities.Openedge.Execution;

namespace Oetools.Builder.Project.Task {
    
    /// <summary>
    /// Base task class for tasks that operates on files and that have targets for aforementioned files.
    /// </summary>
    public abstract class OeTaskFileTarget : OeTaskFile {
        
        private PathList<UoeCompiledFile> CompiledPaths { get; set; }
        
        /// <inheritdoc cref="IOeTaskCompile.SetCompiledFiles"/>
        public void SetCompiledFiles(PathList<UoeCompiledFile> compiledPath) => CompiledPaths = compiledPath;
        
        /// <inheritdoc cref="IOeTaskCompile.GetCompiledFiles"/>
        public PathList<UoeCompiledFile> GetCompiledFiles() => CompiledPaths;
        
    }
}