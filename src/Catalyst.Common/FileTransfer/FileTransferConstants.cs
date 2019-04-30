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

namespace Catalyst.Common.FileTransfer
{
    /// <summary>
    /// File transfer constants
    /// </summary>
    public static class FileTransferConstants
    {
        /// <summary>The expiry minutes of initialization</summary>
        public static readonly int ExpiryMinutes = 1;

        /// <summary>The chunk size in bytes</summary>
        public static readonly int ChunkSize = 1000000;

        /// <summary>The CLI chunk writing wait time</summary>
        public static readonly int CliFileTransferWaitTime = 30;

        /// <summary>The maximum chunk retry count</summary>
        public static readonly int MaxChunkRetryCount = 3;
    }
}
