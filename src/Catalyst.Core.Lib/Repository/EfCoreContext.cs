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

using System;
using Catalyst.Core.Lib.DAO;
using Catalyst.Core.Lib.P2P;
using Catalyst.Core.Lib.P2P.Models;
using Microsoft.EntityFrameworkCore;
using SharpRepository.EfCoreRepository;
using SharpRepository.Repository.Caching;

namespace Catalyst.Core.Lib.Repository
{
    public class EnhancedEfCoreRepository : EfCoreRepository<Peer, string>
    {
        public EnhancedEfCoreRepository(IDbContext dbContext, ICachingStrategy<Peer, string> cachingStrategy = null) :
            base((Microsoft.EntityFrameworkCore.DbContext) dbContext, cachingStrategy)
        { }
    }

    public interface IDbContext : IDisposable
    {
        DbSet<TEntity> Set<TEntity>() where TEntity : class;
    }

    public class EfCoreContext : DbContext, IDbContext
    {
        public EfCoreContext(string connectionString)
            : base(new DbContextOptionsBuilder<EfCoreContext>()
               .UseSqlServer(connectionString).Options) { }
        
        public DbSet<PeerIdDao> PeerIdDao { get; set; }
        public DbSet<Peer> Peer { get; set; }
        public DbSet<PeerIdentifier> PeerIdentifier { get; set; }


        //public DbSet<TransactionBroadcastDao> TransactionBroadcastDao { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder) { }
    }
}

