#region LICENSE

/**
* Copyright (c) 2022 Catalyst Network
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
using Catalyst.Abstractions.Sync.Interfaces;
using Catalyst.Core.Abstractions.Sync;
using Catalyst.Core.Lib.DAO.Ledger;
using Catalyst.Core.Lib.Service;
using Catalyst.Core.Modules.Sync.Manager;
using Catalyst.Core.Modules.Sync.Watcher;
using SharpRepository.InMemoryRepository;
using SharpRepository.MongoDbRepository;
using SharpRepository.Repository;

namespace Catalyst.Core.Modules.Sync
{
    public class SynchroniserModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<Synchroniser>().As<ISynchroniser>().SingleInstance();
            builder.RegisterType<SyncState>().SingleInstance();
            builder.RegisterType<PeerSyncManager>().As<IPeerSyncManager>().SingleInstance();
            builder.RegisterType<DeltaHeightWatcher>().As<IDeltaHeightWatcher>().SingleInstance();
            //builder.RegisterType<InMemoryRepository<DeltaIndexDao, string>>().As<IRepository<DeltaIndexDao, string>>().SingleInstance();
            builder.RegisterType<MongoDbRepository<DeltaIndexDao, string>>().As<IRepository<DeltaIndexDao, string>>().SingleInstance();
            builder.RegisterType<DeltaIndexService>().As<IDeltaIndexService>().SingleInstance();
        }
    }
}
