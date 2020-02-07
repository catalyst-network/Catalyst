using Autofac;
using System;
using Catalyst.Abstractions.Sync.Interfaces;
using Catalyst.Core.Lib.DAO.Ledger;
using Catalyst.Core.Lib.Service;
using Catalyst.Core.Modules.Sync.Modal;
using SharpRepository.InMemoryRepository;
using SharpRepository.Repository;

namespace Catalyst.Core.Modules.Sync
{
    public class SyncModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<Sync>().SingleInstance();
            builder.RegisterType<SyncState>().SingleInstance();
            builder.RegisterType<PeerSyncManager>().As<IPeerSyncManager>().SingleInstance();
            builder.RegisterType<DeltaHeightWatcher>().As<IDeltaHeightWatcher>().SingleInstance();
            builder.RegisterType<InMemoryRepository<DeltaIndexDao>>().As<IRepository<DeltaIndexDao>>().SingleInstance();
            builder.RegisterType<DeltaIndexService>().As<IDeltaIndexService>().SingleInstance();
        }
    }
}
