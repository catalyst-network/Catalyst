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

namespace Lib.P2P.Protocols
{
    /// <summary>
    ///   The result from sending a <see>
    ///       <cref>Catalyst.Ipfs.Core.CoreApi.IGenericApi.PingAsync(MultiFormats.MultiHash,int,System.Threading.CancellationToken)</cref>
    ///   </see>
    ///   .
    /// </summary>
    public class PingResult
    {
        /// <summary>
        ///   Indicates success or failure.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        ///   The round trip time; nano second resolution.
        /// </summary>
        public TimeSpan Time { get; set; }

        /// <summary>
        ///   The text to echo.
        /// </summary>
        public string Text { get; set; }
    }
}
