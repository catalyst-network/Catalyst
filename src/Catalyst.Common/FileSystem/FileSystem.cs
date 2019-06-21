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
using IFileSystem = Catalyst.Common.Interfaces.FileSystem.IFileSystem;
using ILogger = Serilog.ILogger;


namespace Catalyst.Common.FileSystem
{
    public sealed class FileSystem
        : System.IO.Abstractions.FileSystem,
            IFileSystem
    {
        private string _currentPath;
        private readonly ILogger _logger;

        private string _configPointerFilePath => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) 
           + Constants.ConfigFilePointer;

        private bool _checkConfigPointerFile = false;
        public FileSystem()
        {
            _currentPath = string.Empty;
        }

        public FileSystem(bool checkConfigPointerFile, ILogger logger)
        {
            if (checkConfigPointerFile)
            {
                if (!System.IO.File.Exists(_configPointerFilePath))
                {
                    using (System.IO.File.CreateText(_configPointerFilePath)) { }

                    CreateConfigPointerFile(_configPointerFilePath);
                }
                _logger = logger;
            }
            _checkConfigPointerFile = checkConfigPointerFile;
        }

        public DirectoryInfo GetCatalystDataDir()
        {
            if (_checkConfigPointerFile) _currentPath = GetHiddenCatalystDataDir();

            var path = Path.Combine(GetUserHomeDir(), Constants.CatalystDataDir);

            return new DirectoryInfo(_currentPath == string.Empty ? path : _currentPath);
        }

        public bool SetCurrentPath(string path)
        {
            if (new DirectoryInfo(path).Exists)
            {
                _currentPath = path;
                return true;
            }
            return false;
        }

        public async Task<IFileInfo> WriteFileToCdd(string fileName, string contents)
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

        private string GetHiddenCatalystDataDir()
        {
            try
            {   
                using (var sr = new StreamReader(_configPointerFilePath))
                {
                   return sr.ReadToEnd();
                }
            }
            catch (Exception e)
            {
                _logger.Error(e.Message);
            }
            return string.Empty;
        }

        private static string GetUserHomeDir()
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        }

        private static void CreateConfigPointerFile(string path)
        {
            using (StreamWriter writer = new StreamWriter(path))
            {
                var contents = GetUserHomeDir() + Constants.CatalystDataDir;
                writer.Write(contents);
            }
        }
    }
}
