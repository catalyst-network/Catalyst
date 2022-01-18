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
using System.Collections.Generic;

namespace Lib.P2P.Protocols
{
    /// <summary>
    ///   Metadata on <see cref="IPeerProtocol"/>.
    /// </summary>
    public static class ProtocolRegistry
    {
        /// <summary>
        ///   All the peer protocols.
        /// </summary>
        /// <remarks>
        ///   The key is the name and version of the peer protocol, like "/multiselect/1.0.0".
        ///   The value is a Func that returns an new instance of the peer protocol.
        /// </remarks>
        public static Dictionary<string, Func<IPeerProtocol>> Protocols;

        static ProtocolRegistry()
        {
            Protocols = new Dictionary<string, Func<IPeerProtocol>>();
            Register<Multistream1>();
            Register<SecureCommunication.Secio1>();
            Register<Plaintext1>();
            Register<Identify1>();
            Register<Mplex67>();
        }

        /// <summary>
        ///   Register a new protocol.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public static void Register<T>() where T : IPeerProtocol, new()
        {
            T p = new();
            Protocols.Add(p.ToString(), () => new T());
        }

        /// <summary>
        ///   Remove the specified protocol.
        /// </summary>
        /// <param name="protocolName">
        ///   The protocol name to remove.
        /// </param>
        public static void Deregister(string protocolName) { Protocols.Remove(protocolName); }
    }
}
