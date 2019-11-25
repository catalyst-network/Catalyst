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

using Catalyst.Core.Lib.P2P.Models;
using Google.Protobuf;
using System;
using System.Collections.Generic;

namespace Catalyst.Core.Lib.P2P.Service
{
    public interface IPeerService : IDisposable
    {
        Peer Get(string id);
        IEnumerable<Peer> GetAll();
        IEnumerable<Peer> GetActivePeers(int count);
        IEnumerable<Peer> GetRandomPeers(int count);
        IEnumerable<Peer> GetPeersByIpAndPublicKey(ByteString ip, ByteString publicKey);

        void Add(Peer peer);
        void Add(IEnumerable<Peer> peer);

        void Update(Peer peer);

        void Delete(Peer peer);
        void Delete(string id);

        uint DeletePeersByIpAndPublicKey(ByteString ip, ByteString publicKey);

        bool Exists(string id);

        int Count();
    }
}
