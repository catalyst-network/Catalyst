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

using Catalyst.Abstractions.Repository;
using Lib.P2P;
using Lib.P2P;
using Newtonsoft.Json;
using SharpRepository.Repository;

namespace Catalyst.Abstractions.Ledger.Models
{
    /// <summary>
    /// Provides a mapping between the delta number and its <see cref="Cid"/>.
    /// </summary>
    public class DeltaByNumber : IDocument
    {
        public DeltaByNumber()
        {
            // this constructor only exists to allow SharpRepository
            // to store instances of this class
        }

        public DeltaByNumber(long number, Cid deltaId)
        {
            Number = number;
            DeltaId = deltaId;
        }

        public long Number { get; }
        
        public Cid DeltaId { get; }

        [RepositoryPrimaryKey(Order = 1)]
        [JsonProperty("id")]
        public string DocumentId => BuildDocumentId(Number);

        public static string BuildDocumentId(long number) => number.ToString("D");
    }
}
