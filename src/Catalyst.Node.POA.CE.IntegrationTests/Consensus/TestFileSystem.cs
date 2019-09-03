#region LICENSE

/**
* Copyright (c) 2019 Catalyst Network
*
* This file is part of Catalyst.Node <https://github.com/catalyst-network/Catalyst.Node>
*
* Catalyst.Node is free software: you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation, either version 2 of the License, or
* (at your option) any later version.
*
* Catalyst.Node is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
* GNU General Public License for more details.
*
* You should have received a copy of the GNU General Public License
* along with Catalyst.Node. If not, see <https://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.IO;
using System.IO.Abstractions;
using System.Threading.Tasks;
using NSubstitute;
using FileSystem = Catalyst.Core.FileSystem.FileSystem;
using IFileSystem = Catalyst.Abstractions.FileSystem.IFileSystem;

namespace Catalyst.Node.POA.CE.IntegrationTests.Consensus
{
    public class TestFileSystem : IFileSystem
    {
        private readonly IFileSystem _fileSystem;

        public TestFileSystem(string rootPath)
        {
            var rootDirectory = new DirectoryInfo(rootPath);
            _fileSystem = Substitute.ForPartsOf<FileSystem>();
            _fileSystem.GetCatalystDataDir().Returns(rootDirectory);
        }

        public IFile File => _fileSystem.File;

        public IDirectory Directory => _fileSystem.Directory;

        public IFileInfoFactory FileInfo => _fileSystem.FileInfo;

        public IFileStreamFactory FileStream => _fileSystem.FileStream;

        public IPath Path => _fileSystem.Path;

        public IDirectoryInfoFactory DirectoryInfo => _fileSystem.DirectoryInfo;

        public IDriveInfoFactory DriveInfo => _fileSystem.DriveInfo;

        public IFileSystemWatcherFactory FileSystemWatcher => _fileSystem.FileSystemWatcher;

        public DirectoryInfo GetCatalystDataDir() { return _fileSystem.GetCatalystDataDir(); }
        public async Task<IFileInfo> WriteTextFileToCddAsync(string fileName, string contents) { return await _fileSystem.WriteTextFileToCddAsync(fileName, contents); }

        public async Task<IFileInfo> WriteTextFileToCddSubDirectoryAsync(string fileName,
            string subDirectory,
            string contents)
        {
            return await _fileSystem.WriteTextFileToCddSubDirectoryAsync(fileName, subDirectory, contents);
        }

        public bool DataFileExists(string fileName) { return _fileSystem.DataFileExists(fileName); }
        public bool DataFileExistsInSubDirectory(string fileName, string subDirectory) { return _fileSystem.DataFileExistsInSubDirectory(fileName, subDirectory); }
        public string ReadTextFromCddFile(string fileName) { return _fileSystem.ReadTextFromCddFile(fileName); }
        public string ReadTextFromCddSubDirectoryFile(string fileName, string subDirectory) { return _fileSystem.ReadTextFromCddSubDirectoryFile(fileName, subDirectory); }
        public bool SetCurrentPath(string path) { throw new NotImplementedException(); }
    }
}
