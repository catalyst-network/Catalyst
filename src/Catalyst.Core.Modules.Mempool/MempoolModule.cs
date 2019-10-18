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
using Catalyst.Abstractions.Mempool;
using Catalyst.Abstractions.Mempool.Repositories;
using Catalyst.Core.Lib.DAO;
using Catalyst.Core.Modules.Mempool.Repositories;
using SharpRepository.InMemoryRepository;
using SharpRepository.Repository;

namespace Catalyst.Core.Modules.Mempool
{
    public class MempoolModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.Register(c => new InMemoryRepository<TransactionBroadcastDao, string>())
               .As<IRepository<TransactionBroadcastDao, string>>()
               .SingleInstance();
            builder.RegisterType<MempoolRepository>().As<IMempoolRepository<TransactionBroadcastDao>>()
               .SingleInstance();
            builder.RegisterType<Mempool>().As<IMempool<TransactionBroadcastDao>>().SingleInstance();
        }
    }
}
