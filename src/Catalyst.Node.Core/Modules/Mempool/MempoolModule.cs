using System;
using System.Collections.Generic;
using System.Text;
using Autofac;
using SharpRepository.InMemoryRepository;
using SharpRepository.Repository;

namespace Catalyst.Node.Core.Modules.Mempool
{
    public class MempoolModule : JsonConfiguredModule
    {
        public MempoolModule(string configFilePath) : base(configFilePath) { }

        protected override void Load(ContainerBuilder builder)
        {
            base.Load(builder);
            builder.RegisterType<InMemoryRepository<StTxModel>>().As<IRepository<StTxModel>>();
        }
    }
}
