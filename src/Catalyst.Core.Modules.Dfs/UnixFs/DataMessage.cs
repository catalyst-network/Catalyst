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

using ProtoBuf;

#pragma warning disable 0649 // disable warning about unassigned fields
#pragma warning disable 0169 // disable warning about unassigned fields

namespace Catalyst.Core.Modules.Dfs.UnixFs
{
    /// <summary>
    ///   Specifies the type of data.
    /// </summary>
    public enum DataType
    {
        /// <summary>
        ///  Raw data
        /// </summary>
        Raw = 0,

        /// <summary>
        ///   A directory of files. 
        /// </summary>
        Directory = 1,

        /// <summary>
        ///   A file.
        /// </summary>
        File = 2,

        /// <summary>
        ///  Metadata (NYI) 
        /// </summary>
        Metadata = 3,

        /// <summary>
        ///  Symbolic link (NYI) 
        /// </summary>
        Symlink = 4,

        /// <summary>
        ///  NYI 
        /// </summary>
        HamtShard = 5
    };

    /// <summary>
    ///   The ProtoBuf data that is stored in a DAG.
    /// </summary>
    [ProtoContract]
    internal sealed class DataMessage
    {
        /// <summary>
        ///   The type of data.
        /// </summary>
        [ProtoMember(1, IsRequired = true)] internal DataType Type;

        /// <summary>
        ///   The data.
        /// </summary>
        [ProtoMember(2, IsRequired = false)] internal byte[] Data;

        /// <summary>
        ///   The file size.
        /// </summary>
        [ProtoMember(3, IsRequired = false)]
        public ulong? FileSize;

        /// <summary>
        ///  The file size of each block.
        /// </summary>
        [ProtoMember(4, IsRequired = false)]
        public ulong[] BlockSizes;

#pragma warning disable 0649 // disable warning about unassinged fields
        /// <summary>
        ///   NYI
        /// </summary>
        [ProtoMember(5, IsRequired = false)]
        public ulong? HashType;

#pragma warning disable 0649 // disable warning about unassinged fields
        /// <summary>
        ///   NYI
        /// </summary>
        [ProtoMember(6, IsRequired = false)]
        public ulong? Fanout;
    }
}

/*
 *module.exports = `message Data
    {
  enum DataType
    {
        Raw = 0;
        Directory = 1;
        File = 2;
        Metadata = 3;
        Symlink = 4;
        HAMTShard = 5;
    }
    required DataType Type = 1;
  optional bytes Data = 2;
  optional uint64 filesize = 3;
  repeated uint64 blocksizes = 4;
  optional uint64 hashType = 5;
  optional uint64 fanout = 6;
}
message Metadata
{
    required string MimeType = 1;
}
*/
