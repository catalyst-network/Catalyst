#region LICENSE

/**
* Copyright (c) 2024 Catalyst Network
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
using Catalyst.Abstractions.Rpc.Authentication;
using Catalyst.Core.Modules.Authentication.Models;
using Catalyst.Core.Modules.Authentication.Repository;

namespace Catalyst.Core.Modules.Authentication
{
    public class AuthenticationModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<RepositoryAuthenticationStrategy>().As<IAuthenticationStrategy>();
            builder.RegisterType<NoAuthenticationStrategy>().As<IAuthenticationStrategy>();
            builder.RegisterType<AuthCredentialRepository>().As<IAuthCredentialRepository>();
            builder.RegisterType<AuthCredentials>().As<IAuthCredentials>();

            base.Load(builder);
        }
    }
}
