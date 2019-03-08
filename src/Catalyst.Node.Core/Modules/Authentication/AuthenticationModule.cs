using System;
using System.Collections.Generic;
using System.Text;
using Autofac;
using Catalyst.Node.Common.Interfaces;

namespace Catalyst.Node.Core.Modules.Authentication
{
    public class AuthenticationModule : JsonConfiguredModule
    {
        public bool WalletEnabled { get;}

        public AuthenticationModule(string configFilePath, bool walletEnabled) 
            : base(configFilePath)
        {
            this.WalletEnabled = walletEnabled;
        }

        protected override void Load(ContainerBuilder builder)
        {
            base.Load(builder);
            if (WalletEnabled)
            {
                throw new NotImplementedException();
            }
            else
            {
                builder.RegisterType<LocalSignatureProvider>().As<ISignatureProvider>();
            }
        }
    }
}