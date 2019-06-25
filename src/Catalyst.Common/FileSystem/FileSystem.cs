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
//using DirectoryInfoStandard = System.IO.DirectoryInfo;
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

        private bool _checkDataDirPointerFile = false;
        public FileSystem()
        {
            _currentDataDirPointer = string.Empty;
        }

        public FileSystem(bool checkDataDirPointerFile, ILogger logger, string configFilePointer = "", string configDataDir = "\\.Catalyst")
        {
            if (checkDataDirPointerFile)
            {
                DataDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + configDataDir;

                _currentDataDirPointer = configFilePointer == string.Empty ? GetUserHomeDir() + Constants.ConfigFilePointer : configFilePointer;

                if (!new DirectoryInfo(DataDir).Exists)
                {
                    if (CreateDataDirectory(DataDir))
                    {
                        CreateConfigPointerFile(DataDir, configFilePointer);
                    }
                }
                else
                {
                    _currentDataDirPointer = ReadConfigFilePointer();
                }
                _logger = logger;
            }
            _checkDataDirPointerFile = checkDataDirPointerFile;
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
                using (var sr = new StreamReader(DataDir))
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

        private static void CreateConfigPointerFile(string configDirLocation, string configFilePointer)
        {
            using (System.IO.File.Create(configFilePointer)) { }

            SaveConfigPointerFile(configDirLocation, configFilePointer);
        }

        private static void SaveConfigPointerFile(string configDirLocation, string configFilePointer)
        {
            using (StreamWriter writer = new StreamWriter(configFilePointer))
            {
                writer.Write(configDirLocation);
            }
        }

        private bool CreateDataDirectory(string path)
        {
            _dataDirValid = true;
            try
            {
                System.IO.Directory.CreateDirectory(path);
            }
            catch (Exception ex)
            {
                _dataDirValid = false;
            }
            return _dataDirValid;
        }
        private string ReadConfigFilePointer()
        {
            string ln = string.Empty;
            try
            {
                using (StreamReader file = new StreamReader(_currentDataDirPointer))
                {
                    ln = file.ReadLine();
                    file.Close();
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex.ToString());
            }
            return ln;
        }
    
    }
}
