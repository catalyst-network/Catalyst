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
using Catalyst.Core.Lib.DAO.Peer;
using Catalyst.Core.Lib.DAO.Transaction;
using Catalyst.Core.Lib.Service;
using SharpRepository.EfCoreRepository;
using SharpRepository.Repository;

namespace Catalyst.TestUtils.Repository
{
    public sealed class EfCoreDbTestModule : Autofac.Module
    {
        private readonly string _connectionString;
        public EfCoreDbTestModule(string connectionString) { _connectionString = connectionString; }

        protected override void Load(ContainerBuilder builder)
        {
            builder.Register(c => new EfCoreContext(_connectionString)).AsImplementedInterfaces().AsSelf()
               .InstancePerLifetimeScope();

            builder.RegisterType<EfCoreRepository<PeerDao, string>>().As<IRepository<PeerDao, string>>().SingleInstance();
            builder.RegisterType<EfCoreRepository<PublicEntryDao, string>>().As<IRepository<PublicEntryDao, string>>().SingleInstance();
        }
    }
}
