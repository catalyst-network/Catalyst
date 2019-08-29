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
using Catalyst.Protocol.Transaction;
using Newtonsoft.Json;

namespace Catalyst.Core.Util
{
    public static class JsonConverterProviders
    {
        public static readonly IReadOnlyList<JsonConverter> Converters = new List<JsonConverter>
        {
            new JsonProtoObjectConverter<TransactionBroadcast>(),
            new IpEndPointConverter(),
            new IpAddressConverter(),
            new JsonProtoObjectConverter<STTransactionEntry>(),
            new JsonProtoObjectConverter<CFTransactionEntry>(),
            new JsonProtoObjectConverter<EntryRangeProof>()
        }.AsReadOnly();
    }
}
