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
using System.Text;
using Catalyst.Common.Interfaces.Modules.Consensus;

namespace Catalyst.Node.Core.Modules.Consensus
{
    /// <inheritdoc />
    public class DeltaEntity : IDeltaEntity
    {
        /// <inheritdoc />
        public byte[] LocalLedgerState { get; set; }

        /// <inheritdoc />
        public byte[] DeltaHash { get; set; }

        /// <inheritdoc />
        public byte[] Delta { get; set; }

        public static IDeltaEntity Default { get; } = new DeltaEntity {Delta = new byte[0], DeltaHash = new byte[0], LocalLedgerState = new byte[0]};
    }
}
