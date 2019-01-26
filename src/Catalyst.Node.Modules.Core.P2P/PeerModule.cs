using System;
using Autofac;
using Catalyst.Helpers.Util;
using Dawn;

namespace Catalyst.Node.Modules.Core.P2P
{
    /// <summary>
    ///     The Peer Service
    /// </summary>
    public class PeerModule : Module
    {
        /// <summary>
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="mempoolSettings"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public static ContainerBuilder Load(ContainerBuilder builder)
        {
            Guard.Argument(builder, nameof(builder)).NotNull();
//            builder.Register(c => P2P.GetInstance())
//                .As<IP2P>()
//                .SingleInstance();
            return builder;
        }
    }
}