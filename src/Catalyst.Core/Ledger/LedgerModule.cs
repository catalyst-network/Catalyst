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

using Autofac;
using Catalyst.Abstractions.Consensus.Deltas;
using Catalyst.Abstractions.Mempool;
using Catalyst.Core.Ledger.Repository;
using Catalyst.Core.Mempool.Documents;
using Serilog;

namespace Catalyst.Core.Ledger
{
    public class LedgerModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.Register(c => new Ledger(c.Resolve<IAccountRepository>(),
                    c.Resolve<IDeltaHashProvider>(),
                    c.Resolve<IMempool<MempoolDocument>>(),
                    c.Resolve<ILogger>()
                ))
               .As<ILedger>();
        }  
    }
}
