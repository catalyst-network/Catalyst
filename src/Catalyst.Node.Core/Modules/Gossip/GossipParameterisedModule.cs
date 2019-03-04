using System;
using System.Collections.Generic;
using System.Text;
using Autofac;

namespace Catalyst.Node.Core.Modules.Gossip
{
    public class GossipParameterisedModule : JsonConfiguredModule
    {
        private readonly int _numberProvider;

        public GossipParameterisedModule(string configFilePath, int numberProvider) 
            : base(configFilePath)
        {
            _numberProvider = numberProvider;
        }

        protected override void Load(ContainerBuilder builder)
        {
            base.Load(builder);
            if (_numberProvider == 1)
            {
                builder.RegisterType<NumberProvider1>().As<INumberProvider>();
            }
            else
            {
                builder.RegisterType<NumberProvider2>().As<INumberProvider>();
            }
        }
    }
}
