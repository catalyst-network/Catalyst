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
using System.Threading;
using IFileSystem = Catalyst.Common.Interfaces.FileSystem.IFileSystem;
using ILogger = Serilog.ILogger;


namespace Catalyst.Common.FileSystem
{
    public sealed class FileSystem
        : System.IO.Abstractions.FileSystem,
            IFileSystem
    {
        private string _currentDataDirPointer;
        private readonly ILogger _logger;
        private bool _dataDirValid = true;
        private string _dataDir;
        public string DataDir { get { return this._dataDir; } set { _dataDir = value; } }
        public static string FilePointerBaseLocation => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        public FileSystem(ILogger logger, string configDataDir = "")
        {
            _currentDataDirPointer = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Constants.ConfigSubFolder, Constants.ComponentsJsonConfigFile);

            DataDir = File.Exists(_currentDataDirPointer) ?
                GetCurrentDataDir(_currentDataDirPointer) : Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\" + Constants.CatalystDataDir;

            _logger = logger;
        }

        public DirectoryInfo GetCatalystDataDir()
        {
            var path = Path.Combine(GetUserHomeDir(), Constants.CatalystDataDir);

            return new DirectoryInfo(_dataDirValid && string.IsNullOrEmpty(DataDir) == false ? DataDir : path);
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

        public async Task<IFileInfo> WriteFileToCddAsync(string fileName, string contents)
        {
            var fullPath = Path.Combine(GetCatalystDataDir().ToString(), fileName);

            using (var file = File.CreateText(fullPath))
            {
                await file.WriteAsync(contents).ConfigureAwait(false);
                await file.FlushAsync().ConfigureAwait(false);
            }

            return FileInfo.FromFileName(fullPath);
        }

        public bool DataFileExists(string fileName)
        {
            return File.Exists(Path.Combine(GetCatalystDataDir().ToString(), fileName));
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

        private static void SaveConfigPointerFile(string configDirLocation, string configFilePointer)
        {
            var configDataDir = GetCurrentDataDir(configFilePointer);

            configDataDir = PrepDirectoryLocationFormat(configDataDir);
            configDirLocation = PrepDirectoryLocationFormat(configDirLocation);

            string text = System.IO.File.ReadAllText(configFilePointer);
            text = text.Replace(configDataDir, configDirLocation);
            System.IO.File.WriteAllText(configFilePointer, text);          
        }

        private static string PrepDirectoryLocationFormat(string dir)
        {
            var arrayText = dir.Split("\\").ToList();

            var final = arrayText.FirstOrDefault();

            foreach (var item in arrayText.Skip(1))
            {
                final += "\\\\" + item;
            }
            return final;
        }   
    }
}
