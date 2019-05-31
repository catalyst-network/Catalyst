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

namespace Catalyst.Common.FileSystem
{
    public sealed class FileSystem
        : System.IO.Abstractions.FileSystem,
            IFileSystem
    {
        public DirectoryInfo GetCatalystDataDir()
        {
            var path = Path.Combine(GetUserHomeDir(), Constants.CatalystDataDir);
            return new DirectoryInfo(path);
        }

        public async Task<IFileInfo> WriteFileToCDD(string fileName, string contents)
        {
            var fullPath = Path.Combine(GetCatalystDataDir().ToString(), fileName);

            using (var file = File.CreateText(fullPath))
            {
                await file.WriteAsync(contents);
                await file.FlushAsync();
            }

            return FileInfo.FromFileName(fullPath);
        }

        public async Task<bool> DataFileExists(string fileName)
        {
            return File.Exists(Path.Combine(GetCatalystDataDir().ToString(), fileName));
        }
        
        private static string GetUserHomeDir()
        {
            var homePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return homePath;
        }
    }
}
