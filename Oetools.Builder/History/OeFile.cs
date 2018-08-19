﻿using System;
using System.Xml.Serialization;
using Oetools.Builder.Utilities;
using Oetools.Utilities.Lib;

namespace Oetools.Builder.History {
    
    [Serializable]
    public class OeFile {
        
        public OeFile() { }

        public OeFile(string sourcePath) {
            SourcePath = sourcePath;
        }

        /// <summary>
        /// The relative path of the source file
        /// </summary>
        [XmlAttribute(AttributeName = "SourcePath")]
        [BaseDirectory(Type = BaseDirectoryType.SourceDirectory)]
        public string SourcePath { get; set; }
        
        /// <summary>
        /// Can be different from <see cref="SourcePath"/> for instance in the case of a .p, <see cref="SourcePathForTaskExecution"/>
        /// will be set to the path of the .r code to copy instead of the actual source path
        /// </summary>
        [XmlIgnore]
        internal string SourcePathForTaskExecution { get; set; }

        [XmlAttribute(AttributeName = "LastWriteTime")]
        public DateTime LastWriteTime { get; set; }

        [XmlAttribute(AttributeName = "Size")]
        public long Size { get; set; }

        /// <summary>
        ///     MD5
        /// </summary>
        [XmlAttribute(AttributeName = "Md5")]
        public string Hash { get; set; }
        
        /// <summary>
        /// Represents the state of the file for this build compare to the previous one
        /// </summary>
        [XmlElement(ElementName = "State")]
        public OeFileState State { get; set; }

        public OeFile GetDeepCopy() {
            return (OeFile) Utils.DeepCopyPublicProperties(this, typeof(OeFile));
        }
    }
}