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

using System.Collections.Generic;

namespace Catalyst.Core.Modules.Dfs.LinkedData
{
    /// <summary>
    ///   Metadata on <see cref="ILinkedDataFormat"/>.
    /// </summary>
    public static class IpldRegistry
    {
        /// <summary>
        ///   All the supported IPLD formats.
        /// </summary>
        /// <remarks>
        ///   The key is the multicodec name.
        ///   The value is an object that implements <see cref="ILinkedDataFormat"/>.
        /// </remarks>
        public static Dictionary<string, ILinkedDataFormat> Formats;

        static IpldRegistry()
        {
            Formats = new Dictionary<string, ILinkedDataFormat>();
            Register<CborFormat>("dag-cbor");
            Register<ProtobufFormat>("dag-pb");
            Register<RawFormat>("raw");
        }

        /// <summary>
        ///   Register a new IPLD format.
        /// </summary>
        /// <typeparam name="T">
        ///   A Type that implements <see cref="ILinkedDataFormat"/>.
        /// </typeparam>
        /// <param name="name">
        ///   The multicodec name.
        /// </param>
        public static void Register<T>(string name) where T : ILinkedDataFormat, new() { Formats.Add(name, new T()); }

        /// <summary>
        ///   Remove the IPLD format.
        /// </summary>
        /// <param name="name">
        ///   The multicodec name.
        /// </param>
        public static void Deregister(string name) { Formats.Remove(name); }
    }
}
