using Autofac;
using System;
using Catalyst.Abstractions.Sync.Interfaces;
using Catalyst.Core.Lib.DAO.Ledger;
using Catalyst.Core.Lib.Service;
using SharpRepository.InMemoryRepository;
using SharpRepository.Repository;

namespace Catalyst.Core.Modules.Sync
{
    public class SyncModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<Sync>();
            builder.RegisterType<PeerSyncManager>().As<IPeerSyncManager>();
            builder.RegisterType<DeltaHeightWatcher>().As<IDeltaHeightWatcher>();
            builder.RegisterType<InMemoryRepository<DeltaIndexDao>>().As<IRepository<DeltaIndexDao>>().SingleInstance();
            builder.RegisterType<DeltaIndexService>().As<IDeltaIndexService>().SingleInstance();
        }
    }
}
