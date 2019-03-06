using System;
using System.Collections.Generic;
using System.Text;
using Autofac;
using Catalyst.Node.Common.Helpers.Cryptography;
using Catalyst.Node.Common.Helpers.Keystore;
using Catalyst.Node.Common.Modules.Authentication;

namespace Catalyst.Node.Core.Modules.Authentication
{
    public class AuthenticationModule : JsonConfiguredModule
    {
        private readonly ICryptoContext _context;
        public bool WalletEnabled { get; set; }

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