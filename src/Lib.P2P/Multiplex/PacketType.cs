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

namespace Lib.P2P.Multiplex
{
    /// <summary>
    ///   The purpose of the multiplex message.
    /// </summary>
    /// <seealso cref="Header"/>
    public enum PacketType : byte
    {
        /// <summary>
        ///   Create a new stream.
        /// </summary>
        NewStream = 0,

        /// <summary>
        ///   A message from the "receiver".
        /// </summary>
        MessageReceiver = 1,

        /// <summary>
        ///   A message from the "initiator".
        /// </summary>
        MessageInitiator = 2,

        /// <summary>
        ///   Close the stream from the "receiver".
        /// </summary>
        CloseReceiver = 3,

        /// <summary>
        ///   Close the stream from the "initiator".
        /// </summary>
        CloseInitiator = 4,

        /// <summary>
        ///   Reset the stream from the "receiver".
        /// </summary>
        ResetReceiver = 5,

        /// <summary>
        ///   Reset the stream from the "initiator".
        /// </summary>
        ResetInitiator = 6
    }
}
