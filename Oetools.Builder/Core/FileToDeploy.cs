﻿// ========================================================================
// Copyright (c) 2017 - Julien Caillon (julien.caillon@gmail.com)
// This file (FileToDeploy.cs) is part of csdeployer.
// 
// csdeployer is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// csdeployer is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with csdeployer. If not, see <http://www.gnu.org/licenses/>.
// ========================================================================

using System;
using System.IO;
using System.Text.RegularExpressions;
using Oetools.Builder.Core.Rule;
using Oetools.Utilities.Archive;
using Oetools.Utilities.Archive.Cab;
using Oetools.Utilities.Archive.Prolib;
using Oetools.Utilities.Archive.Zip;
using Oetools.Utilities.Ftp;
using Oetools.Utilities.Ftp.Archiver;
using Oetools.Utilities.Lib;
using Oetools.Utilities.Lib.Extension;

namespace Oetools.Builder.Core {
    
    /// <summary>
    ///     Represents a file that needs to be deployed
    /// </summary>
    public class FileToDeploy {
        /// <summary>
        ///     Constructor
        /// </summary>
        public FileToDeploy(string sourcePath, string targetBasePath, DeployTransferRule rule) {
            Origin = sourcePath;
            TargetBasePath = targetBasePath;
            RuleReference = rule;
        }

        /// <summary>
        ///     Deploy this file
        /// </summary>
        protected virtual bool TryDeploy() {
            return true;
        }

        public static FileToDeploy New(DeployType deployType, string sourcePath, string targetBasePath, DeployTransferRule rule) {
            switch (deployType) {
                case DeployType.Prolib:
                    return new FileToDeployProlib(sourcePath, targetBasePath, rule);
                case DeployType.Zip:
                    return new FileToDeployZip(sourcePath, targetBasePath, rule);
                case DeployType.DeleteInProlib:
                    return new FileToDeployDeleteInProlib(sourcePath, targetBasePath, rule);
                case DeployType.Ftp:
                    return new FileToDeployFtp(sourcePath, targetBasePath, rule);
                case DeployType.Delete:
                    return new FileToDeployDelete(sourcePath, targetBasePath, rule);
                case DeployType.Copy:
                    return new FileToDeployCopy(sourcePath, targetBasePath, rule);
                case DeployType.Move:
                    return new FileToDeployMove(sourcePath, targetBasePath, rule);
                case DeployType.Cab:
                    return new FileToDeployCab(sourcePath, targetBasePath, rule);
                case DeployType.CopyFolder:
                    return new FileToDeployCopyFolder(sourcePath, targetBasePath, rule);
                case DeployType.DeleteFolder:
                    return new FileToDeployDeleteFolder(sourcePath, targetBasePath, rule);
                default:
                    throw new ArgumentOutOfRangeException("deployType", deployType, null);
            }
        }

        /// <summary>
        ///     If this file has been added through a rule, this holds the rule reference (can be null)
        /// </summary>
        public DeployTransferRule RuleReference { get; set; }

        /// <summary>
        ///     target path computed from the deployment rules
        /// </summary>
        public string TargetBasePath { get; set; }

        /// <summary>
        ///     The path of input file that was originally compiled to trigger this move (can be equal to From)
        /// </summary>
        public string Origin { get; set; }

        /// <summary>
        ///     Need to deploy this file FROM this path
        /// </summary>
        public string From { get; set; }

        /// <summary>
        ///     Need to deploy this file TO this path
        /// </summary>
        public string To { get; set; }

        /// <summary>
        ///     true if the transfer went fine
        /// </summary>
        public bool IsOk { get; set; }

        /// <summary>
        ///     Type of transfer
        /// </summary>
        public virtual DeployType DeployType {
            get { return DeployType.Copy; }
        }

        /// <summary>
        ///     Null if no errors, otherwise it contains the description of the error that occurred for this file
        /// </summary>
        public string DeployError { get; set; }

        /// <summary>
        ///     A directory that must exist or be created for this deployment (can be null if nothing to do)
        /// </summary>
        public virtual string DirectoryThatMustExist {
            get { return Path.GetDirectoryName(To); }
        }

        /// <summary>
        ///     This is used to group the FileToDeploy during the creation of the deployment report,
        ///     use this in addition with GroupHeaderToString
        /// </summary>
        public virtual string GroupKey {
            get { return Path.GetDirectoryName(To); }
        }

        /// <summary>
        ///     Indicate whether or not this deployment can be parallelized
        /// </summary>
        public virtual bool CanBeParallelized {
            get { return true; }
        }

        /// <summary>
        ///     Indicates if this deployment is actually a deletion of a file
        /// </summary>
        public virtual bool IsDeletion {
            get { return false; }
        }

        public virtual FileToDeploy Set(string from, string to) {
            From = from;
            To = to;
            return this;
        }

        /// <summary>
        ///     Returns a "copy" (only target path and those inputs are copied) if this object, setting properties in the meantime
        /// </summary>
        public virtual FileToDeploy Copy(string from, string to) {
            return New(DeployType, Origin, TargetBasePath, RuleReference).Set(from, to);
        }

        /// <summary>
        ///     Deploy this file
        /// </summary>
        public virtual bool DeploySelf() {
            if (IsOk)
                return false;
            IsOk = TryDeploy();
            return IsOk;
        }
    }

    /// <summary>
    ///     Uses only TO
    /// </summary>
    public class FileToDeployDelete : FileToDeploy {
        public FileToDeployDelete(string sourcePath, string targetBasePath, DeployTransferRule rule) : base(sourcePath, targetBasePath, rule) { }

        protected override bool TryDeploy() {
            try {
                if (string.IsNullOrEmpty(To) || !File.Exists(To))
                    return true;
                File.Delete(To);
            } catch (Exception e) {
                DeployError = "Impossible de supprimer le fichier " + To.Quoter() + " : \"" + e.Message + "\"";
                return false;
            }
            return true;
        }

        /// <summary>
        ///     Type of transfer
        /// </summary>
        public override DeployType DeployType {
            get { return DeployType.Delete; }
        }

        /// <summary>
        ///     A directory that must exist or be created for this deployment
        /// </summary>
        public override string DirectoryThatMustExist {
            get { return null; }
        }

        public override string GroupKey {
            get { return "Deleted"; }
        }

        /// <summary>
        ///     Indicates if this deployment is actually a deletion of a file
        /// </summary>
        public override bool IsDeletion {
            get { return true; }
        }
    }

    public class FileToDeployDeleteFolder : FileToDeploy {
        public FileToDeployDeleteFolder(string sourcePath, string targetBasePath, DeployTransferRule rule) : base(sourcePath, targetBasePath, rule) { }

        protected override bool TryDeploy() {
            try {
                if (string.IsNullOrEmpty(To) || !Directory.Exists(To))
                    return true;
                Directory.Delete(To, true);
            } catch (Exception e) {
                DeployError = "Impossible de supprimer le dossier " + To.Quoter() + " : \"" + e.Message + "\"";
                return false;
            }
            return true;
        }

        /// <summary>
        ///     Type of transfer
        /// </summary>
        public override DeployType DeployType {
            get { return DeployType.DeleteFolder; }
        }

        /// <summary>
        ///     A directory that must exist or be created for this deployment
        /// </summary>
        public override string DirectoryThatMustExist {
            get { return null; }
        }

        public override string GroupKey {
            get { return "Deleted"; }
        }

        /// <summary>
        ///     Indicate whether or not this deployment can be parallelized
        /// </summary>
        public override bool CanBeParallelized {
            get { return false; }
        }

        /// <summary>
        ///     Indicates if this deployment is actually a deletion of a file
        /// </summary>
        public override bool IsDeletion {
            get { return true; }
        }
    }

    /// <summary>
    ///     A class for files that need to be deploy in "packs" (i.e. .zip, FTP)
    /// </summary>
    public abstract class FileToDeployInPack : FileToDeploy, IFileToArchive {
        /// <summary>
        ///     Constructor
        /// </summary>
        protected FileToDeployInPack(string sourcePath, string targetBasePath, DeployTransferRule rule) : base(sourcePath, targetBasePath, rule) { }

        /// <summary>
        /// The path of this file
        /// </summary>
        public string SourcePath {
            get { return From; }
            set { From = value; }
        }

        /// <summary>
        ///     Path to the pack in which we need to include this file
        /// </summary>
        public string ArchivePath { get; set; }

        /// <summary>
        ///     The relative path of the file within the pack
        /// </summary>
        public string RelativePathInArchive { get; set; }

        /// <summary>
        ///     A directory that must exist or be created for this deployment
        /// </summary>
        public override string DirectoryThatMustExist {
            get { return Path.GetDirectoryName(ArchivePath); }
        }

        /// <summary>
        ///     Path to the pack file
        /// </summary>
        public override string GroupKey {
            get { return ArchivePath ?? To; }
        }

        /// <summary>
        ///     Extension of the archive file
        /// </summary>
        public virtual string PackExt {
            get { return ".arc"; }
        }

        /// <summary>
        ///     Returns a new archive info
        /// </summary>
        internal virtual IArchiver NewArchive(Deployer deployer) {
            return null;
        }

        /// <summary>
        ///     Saves an exception in the deploy error
        /// </summary>
        public virtual void RegisterArchiveException(Exception e) {
            IsOk = false;
            DeployError = "Problème avec le pack cible " + ArchivePath.Quoter() + " : \"" + e.Message + "\"";
        }

        /// <summary>
        ///     Allows to check the source file before putting this fileToDeploy in a pack
        /// </summary>
        public virtual bool IfFromFileExists() {
            if (!File.Exists(From)) {
                DeployError = "Le fichier source " + From.Quoter() + " n'existe pas";
                return false;
            }
            return true;
        }

        public override FileToDeploy Set(string from, string to) {
            var pos = to.LastIndexOf(PackExt + @"\", StringComparison.CurrentCultureIgnoreCase);
            if (pos < 0)
                pos = to.LastIndexOf(PackExt, StringComparison.CurrentCultureIgnoreCase);
            if (pos >= 0) {
                pos += PackExt.Length;
                ArchivePath = to.Substring(0, pos);
                RelativePathInArchive = pos + 1 < to.Length ? to.Substring(pos + 1) : "\\";
            }
            return base.Set(from, to);
        }
    }

    /// <summary>
    ///     Uses only FROM (to compute the PACKPATH) and the rule deployment target (to compute RELATIVEPATHINPACK)
    ///     or only TO
    /// </summary>
    public class FileToDeployDeleteInProlib : FileToDeployInPack {
        public FileToDeployDeleteInProlib(string sourcePath, string targetBasePath, DeployTransferRule rule) : base(sourcePath, targetBasePath, rule) { }

        /// <summary>
        ///     Type of transfer
        /// </summary>
        public override DeployType DeployType {
            get { return DeployType.DeleteInProlib; }
        }

        public override string PackExt {
            get { return ".pl"; }
        }

        /// <summary>
        ///     A directory that must exist or be created for this deployment
        /// </summary>
        public override string DirectoryThatMustExist {
            get { return null; }
        }

        public override string GroupKey {
            get { return "Deleted .pl"; }
        }

        /// <summary>
        ///     Indicates if this deployment is actually a deletion of a file
        /// </summary>
        public override bool IsDeletion {
            get { return true; }
        }

        /// <summary>
        ///     Allows to check the source file before putting this fileToDeploy in a pack
        /// </summary>
        public override bool IfFromFileExists() {
            return true;
        }

        /// <summary>
        ///     Returns a new archive info
        /// </summary>
        internal override IArchiver NewArchive(Deployer deployer) {
            return new ProlibArchiveDeleter(deployer.ProlibPath);
        }

        public override FileToDeploy Set(string from, string to) {
            if (RuleReference != null) {
                From = from;
                ArchivePath = from;
                RelativePathInArchive = RuleReference.DeployTarget;
                To = Path.Combine(from, RuleReference.DeployTarget);
            } else {
                base.Set(from, to);
            }
            return this;
        }
    }

    public class FileToDeployCab : FileToDeployInPack {
        /// <summary>
        ///     Type of transfer
        /// </summary>
        public override DeployType DeployType {
            get { return DeployType.Cab; }
        }

        public override string PackExt {
            get { return ".cab"; }
        }

        public FileToDeployCab(string sourcePath, string targetBasePath, DeployTransferRule rule) : base(sourcePath, targetBasePath, rule) { }

        /// <summary>
        ///     Returns a new archive info
        /// </summary>
        internal override IArchiver NewArchive(Deployer deployer) {
            return new CabArchiver();
        }
    }

    public class FileToDeployProlib : FileToDeployInPack {
        /// <summary>
        ///     Type of transfer
        /// </summary>
        public override DeployType DeployType {
            get { return DeployType.Prolib; }
        }

        public override string PackExt {
            get { return ".pl"; }
        }

        public FileToDeployProlib(string sourcePath, string targetBasePath, DeployTransferRule rule) : base(sourcePath, targetBasePath, rule) { }

        /// <summary>
        ///     Returns a new archive info
        /// </summary>
        internal override IArchiver NewArchive(Deployer deployer) {
            return new ProlibArchiver(deployer.ProlibPath);
        }
    }

    public class FileToDeployZip : FileToDeployInPack {
        /// <summary>
        ///     Type of transfer
        /// </summary>
        public override DeployType DeployType {
            get { return DeployType.Zip; }
        }

        public override string PackExt {
            get { return ".zip"; }
        }

        public FileToDeployZip(string sourcePath, string targetBasePath, DeployTransferRule rule) : base(sourcePath, targetBasePath, rule) { }

        /// <summary>
        ///     Returns a new archive info
        /// </summary>
        internal override IArchiver NewArchive(Deployer deployer) {
            return new ZipArchiver();
        }
    }

    public class FileToDeployFtp : FileToDeployInPack {
        /// <summary>
        ///     Constructor
        /// </summary>
        public FileToDeployFtp(string sourcePath, string targetBasePath, DeployTransferRule rule) : base(sourcePath, targetBasePath, rule) { }

        /// <summary>
        ///     Type of transfer
        /// </summary>
        public override DeployType DeployType {
            get { return DeployType.Ftp; }
        }

        /// <summary>
        ///     Path to the pack file
        /// </summary>
        public override string GroupKey {
            get { return ArchivePath ?? To; }
        }

        /// <summary>
        ///     A directory that must exist or be created for this deployment
        /// </summary>
        public override string DirectoryThatMustExist {
            get { return null; }
        }

        public override FileToDeploy Set(string from, string to) {
            to.ParseFtpAddress(out var ftpBaseUri, out _, out _, out _, out _, out var relativePath);
            ArchivePath = ftpBaseUri;
            RelativePathInArchive = relativePath;
            return base.Set(from, to);
        }

        /// <summary>
        ///     Returns a new archive info
        /// </summary>
        internal override IArchiver NewArchive(Deployer deployer) {
            return new FtpArchiver();
        }

        /// <summary>
        ///     Saves an exception in the deploy error
        /// </summary>
        public override void RegisterArchiveException(Exception e) {
            IsOk = false;
            DeployError = "Problème avec le serveur FTP " + ArchivePath.Quoter() + " : \"" + e.Message + "\"";
        }
    }

    public class FileToDeployCopyFolder : FileToDeploy {
        public FileToDeployCopyFolder(string sourcePath, string targetBasePath, DeployTransferRule rule) : base(sourcePath, targetBasePath, rule) { }

        protected override bool TryDeploy() {
            try {
                if (!Directory.Exists(From)) {
                    DeployError = "Le dossier source " + From.Quoter() + " n'existe pas";
                    return false;
                }
                // make sure that both From and To finish with \
                From = Path.GetFullPath(From);
                To = Path.GetFullPath(To);
                // create all of the directories
                foreach (var dirPath in Directory.EnumerateDirectories(From, "*", SearchOption.AllDirectories)) Directory.CreateDirectory(dirPath.Replace(From, To));
                // copy all the files & replaces any files with the same name
                foreach (var newPath in Directory.EnumerateFiles(From, "*.*", SearchOption.AllDirectories)) File.Copy(newPath, newPath.Replace(From, To), true);
            } catch (Exception e) {
                DeployError = "Impossible de copier le dossier " + From.Quoter() + " vers " + To.Quoter() + " : \"" + e.Message + "\"";
                return false;
            }
            return true;
        }

        /// <summary>
        ///     Type of transfer
        /// </summary>
        public override DeployType DeployType {
            get { return DeployType.CopyFolder; }
        }

        /// <summary>
        ///     This is used to group the FileToDeploy during the creation of the deployment report,
        ///     use this in addition with GroupHeaderToString
        /// </summary>
        public override string GroupKey {
            get { return To; }
        }

        /// <summary>
        ///     A directory that must exist or be created for this deployment
        /// </summary>
        public override string DirectoryThatMustExist {
            get { return null; }
        }
    }

    public class FileToDeployCopy : FileToDeploy {
        /// <summary>
        ///     This can be set to true for a file deployed during step 0 (compilation), if the last
        ///     deployment is a Copy, we make it a Move because this allows us to directly compile were
        ///     we need to finally move it instead of compiling then copying...
        /// </summary>
        public bool FinalDeploy { get; set; }

        /// <summary>
        ///     Type of transfer
        /// </summary>
        public override DeployType DeployType {
            get { return FinalDeploy ? DeployType.Move : DeployType.Copy; }
        }

        public FileToDeployCopy(string sourcePath, string targetBasePath, DeployTransferRule rule) : base(sourcePath, targetBasePath, rule) { }

        protected override bool TryDeploy() {
            try {
                if (From.Equals(To))
                    return true;
                if (!File.Exists(From)) {
                    DeployError = "Le fichier source " + From.Quoter() + " n'existe pas";
                    return false;
                }
                File.Copy(From, To, true);
            } catch (Exception e) {
                DeployError = "Impossible de copier " + From.Quoter() + " vers  " + To.Quoter() + " : \"" + e.Message + "\"";
                return false;
            }
            return true;
        }
    }

    public class FileToDeployMove : FileToDeploy {
        /// <summary>
        ///     Type of transfer
        /// </summary>
        public override DeployType DeployType {
            get { return DeployType.Move; }
        }

        public FileToDeployMove(string sourcePath, string targetBasePath, DeployTransferRule rule) : base(sourcePath, targetBasePath, rule) { }

        protected override bool TryDeploy() {
            try {
                if (From.Equals(To))
                    return true;
                if (!File.Exists(From)) {
                    DeployError = "Le fichier source " + From.Quoter() + " n'existe pas";
                    return false;
                }
                File.Delete(To);
                File.Move(From, To);
            } catch (Exception e) {
                DeployError = "Impossible de déplacer " + From.Quoter() + " vers  " + To.Quoter() + " : \"" + e.Message + "\"";
                return false;
            }
            return true;
        }
    }
}