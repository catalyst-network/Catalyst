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

using System.IO;
using Catalyst.Abstractions.FileSystem;
using SharpRepository.XmlRepository;

namespace Catalyst.Core.Repository
{
    /// <summary>
    /// Xml Repository where base folder is derived from the file system <see cref="IFileSystem"/>
    /// </summary>
    /// <typeparam name="T">Type of object</typeparam>
    /// <seealso cref="SharpRepository.XmlRepository.XmlRepository{T}" />
    public class FileSystemAwareXmlRepository<T> : XmlRepository<T, string> where T : class, new()
    {
        public FileSystemAwareXmlRepository(IFileSystem fileSystem, string path = "") : base(Path.Combine(fileSystem.GetCatalystDataDir().ToString(), path)) { }
    }
}
