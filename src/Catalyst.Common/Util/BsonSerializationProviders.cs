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

using Catalyst.Common.P2P;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Transaction;
using Google.Protobuf;
using MongoDB.Bson.Serialization;

namespace Catalyst.Common.Util
{
    public static class BsonSerializationProviders
    {
        private static bool _initialized = false;

        public static void Init()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;
            
            if (!BsonClassMap.IsClassMapRegistered(typeof(PeerIdentifier)))
            {
                BsonClassMap.RegisterClassMap<PeerIdentifier>();
            }

            AddSerializer<TransactionBroadcast>();
            AddSerializer<PeerId>();
            AddSerializer<STTransactionEntry>();
            AddSerializer<CFTransactionEntry>();
            AddSerializer<EntryRangeProof>();
        }
        
        static void AddSerializer<TType>() where TType : IMessage, new()
        {
            BsonSerializer.RegisterSerializer(typeof(TType),
                new ProtoBsonSerializer<TType>());
        }
    }
}
