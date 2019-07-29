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
using Catalyst.Common.Config;
using Microsoft.Extensions.Configuration;
using System.Linq;
using IFileSystem = Catalyst.Common.Interfaces.FileSystem.IFileSystem;
using ILogger = Serilog.ILogger;


namespace Catalyst.Common.FileSystem
{
    public class FileSystem
        : System.IO.Abstractions.FileSystem,
            IFileSystem
    {
        private string _currentDataDirPointer;
        private string _dataDir;     

        public FileSystem()
        {
            _currentDataDirPointer  = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Constants.ConfigSubFolder, Constants.ComponentsJsonConfigFile);

            _dataDir = File.Exists(_currentDataDirPointer) ?
                GetCurrentDataDir(_currentDataDirPointer) : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), Constants.CatalystDataDir);
        }
        public virtual DirectoryInfo GetCatalystDataDir()
        {
            var path = Path.Combine(GetUserHomeDir(), Constants.CatalystDataDir);

            return new DirectoryInfo(string.IsNullOrEmpty(_dataDir) == false ? _dataDir : path);
        }
        public bool SetCurrentPath(string path)
        {
            if (new DirectoryInfo(path).Exists)
            {
                SaveConfigPointerFile(path, _currentDataDirPointer);

                _dataDir = path;
              
                return true;
            }
            return false;
        }

        public Task<IFileInfo> WriteTextFileToCddAsync(string fileName, string contents)
        {
            var fullPath = Path.Combine(GetCatalystDataDir().FullName, fileName);

            return WriteFileToPathAsync(fullPath, contents);
        }

        public Task<IFileInfo> WriteTextFileToCddSubDirectoryAsync(string fileName, string subDirectory, string contents)
        {
            var fullPath = Path.Combine(GetCatalystDataDir().FullName, subDirectory, fileName);

            return WriteFileToPathAsync(fullPath, contents);
        }

        private async Task<IFileInfo> WriteFileToPathAsync(string path, string contents)
        {
            var fileInfo = FileInfo.FromFileName(path);
            if (!Directory.Exists(fileInfo.DirectoryName))
            {
                Directory.CreateDirectory(fileInfo.DirectoryName);
            }

            using (var file = File.CreateText(path))
            {
                await file.WriteAsync(contents).ConfigureAwait(false);
                await file.FlushAsync().ConfigureAwait(false);
            }

            return FileInfo.FromFileName(path);
        }

        public bool DataFileExists(string fileName)
        {
            return File.Exists(Path.Combine(GetCatalystDataDir().FullName, fileName));
        }

        public bool DataFileExistsInSubDirectory(string fileName, string subDirectory)
        {
            return File.Exists(Path.Combine(GetCatalystDataDir().FullName, subDirectory, fileName));
        }

        private static string GetCurrentDataDir(string configFilePointer)
        {
            var configurationRoot = new ConfigurationBuilder().AddJsonFile(configFilePointer).Build();

            return configurationRoot.GetSection("components").GetChildren()
                .Select(p => p.GetSection("parameters:configDataDir").Value).ToArray()
                .Where(m => string.IsNullOrEmpty(m) == false).FirstOrDefault();
        }

        private static string GetUserHomeDir()
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        }

        public string ReadTextFromCddFile(string fileName)
        {
            var path = Path.Combine(GetCatalystDataDir().FullName, fileName);
            return ReadTextFromFile(path);
        }

        public string ReadTextFromCddSubDirectoryFile(string fileName, string subDirectory)
        {
            var path = Path.Combine(GetCatalystDataDir().FullName, subDirectory, fileName);
            return ReadTextFromFile(path);
        }

        private string ReadTextFromFile(string filePath)
        {
            return File.Exists(filePath) ? File.ReadAllText(filePath) : null;
        }

        private void SaveConfigPointerFile(string configDirLocation, string configFilePointer)
        {
            var configDataDir = GetCurrentDataDir(configFilePointer);

            configDataDir = PrepDirectoryLocationFormatB(configDataDir);
            configDirLocation = PrepDirectoryLocationFormatB(configDirLocation);

            string text = System.IO.File.ReadAllText(configFilePointer);
            text = text.Replace(configDataDir, configDirLocation);
            System.IO.File.WriteAllText(configFilePointer, text);
        }

        private static string PrepDirectoryLocationFormat(string dir)
        {
            //Can Path combine address this, issue
            var arrayText = dir.Split("\\").ToList();

            var final = arrayText.FirstOrDefault();

            foreach (var item in arrayText.Skip(1))
            {
                final += "\\\\" + item;
            }
            return final;
        }

        private string PrepDirectoryLocationFormatB(string dir)
        {
            //Can Path combine address this, issue
            var arrayText = dir.Split(System.IO.Path.DirectorySeparatorChar.ToString()).ToList();

            var final = arrayText.FirstOrDefault();

            foreach (var item in arrayText.Skip(1))
            {
                //final += "\\\\" + item;
                final += Path.Combine(System.IO.Path.DirectorySeparatorChar.ToString(), item);
            }
            return final;
        }
    }
}
