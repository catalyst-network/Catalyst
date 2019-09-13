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
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Catalyst.Core.Lib.P2P.Models;
using Google.Protobuf;
using Microsoft.EntityFrameworkCore;
using SharpRepository.EfCoreRepository;
using SharpRepository.Repository;
using SharpRepository.Repository.Caching;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Catalyst.Core.Lib.Repository
{
    public interface IDbContext : IDisposable
    {
        DbSet<TEntity> Set<TEntity>() where TEntity : class;
    }

    //public class EnhancedEfCoreRepository : EfCoreRepository<MempoolTempDocument, string>
    //{
    //    public EnhancedEfCoreRepository(IDbContext dbContext, ICachingStrategy<MempoolTempDocument, string> cachingStrategy = null) :
    //        base((Microsoft.EntityFrameworkCore.DbContext) dbContext, cachingStrategy)
    //    { }
    //}

    public class EnhancedEfCoreRepository : EfCoreRepository<Contact, string>
    {
        public EnhancedEfCoreRepository(IDbContext dbContext, ICachingStrategy<Contact, string> cachingStrategy = null) :
            base((Microsoft.EntityFrameworkCore.DbContext)dbContext, cachingStrategy)
        { }
    }

    public class EfCoreContext : Microsoft.EntityFrameworkCore.DbContext, IDbContext
    {
        public EfCoreContext(string connectionString)
            : base(new DbContextOptionsBuilder<EfCoreContext>()
               .UseSqlServer(connectionString).Options)
        { }

        //public Microsoft.EntityFrameworkCore.DbSet<Contact> Contacts { get; set; }
        //public Microsoft.EntityFrameworkCore.DbSet<PhoneNumber> PhoneNumbers { get; set; }
        //public Microsoft.EntityFrameworkCore.DbSet<EmailAddress> EmailAddresses { get; set; }
        //public Microsoft.EntityFrameworkCore.DbSet<MempoolDocument> MempoolDocument { get; set; }
        //public Microsoft.EntityFrameworkCore.DbSet<ByteString> ByteStr { get; set; }
        //public Microsoft.EntityFrameworkCore.DbSet<byte[]> ByteItem { get; set; }
        //public Microsoft.EntityFrameworkCore.DbSet<MempoolTempDocument> MempoolTempDocument { get; set; }
        //public Microsoft.EntityFrameworkCore.DbSet<PeerInfo> PeerInfo { get; set; }

        //public Microsoft.EntityFrameworkCore.DbSet<PeerId> PeerId { get; set; }

        public Microsoft.EntityFrameworkCore.DbSet<PeerIdDb> PeerIdDb { get; set; }

        public Microsoft.EntityFrameworkCore.DbSet<Peer> Peer { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var converter = new ValueConverter<ByteString, string>(
                v => v.ToBase64(),
                v => ByteString.FromBase64(v));

            //Converts//

            //modelBuilder
            //   .Entity<Peer>()
            //   .Property(e => e.PeerIdentifier)
            //   .HasConversion(converter);

            //modelBuilder
            //   .Entity<PeerId>()
            //   .Property(e => e.ClientVersion)
            //   .HasConversion(converter);

            //modelBuilder
            //   .Entity<PeerId>()
            //   .Property(e => e.Ip)
            //   .HasConversion(converter);

            //modelBuilder
            //   .Entity<PeerId>()
            //   .Property(e => e.ClientId)
            //   .HasConversion(converter);

            //modelBuilder
            //   .Entity<PeerId>()
            //   .Property(e => e.Port)
            //   .HasConversion(converter);

            //modelBuilder
            //   .Entity<PeerId>()
            //   .Property(e => e.PublicKey)
            //   .HasConversion(converter);


            //---------------------------------------//
            modelBuilder
               .Entity<PeerIdDb>()
               .Property(e => e.ClientVersion)
               .HasConversion(converter);

            modelBuilder
               .Entity<PeerIdDb>()
               .Property(e => e.Ip)
               .HasConversion(converter);

            modelBuilder
               .Entity<PeerIdDb>()
               .Property(e => e.ClientId)
               .HasConversion(converter);

            modelBuilder
               .Entity<PeerIdDb>()
               .Property(e => e.Port)
               .HasConversion(converter);

            modelBuilder
               .Entity<PeerIdDb>()
               .Property(e => e.PublicKey)
               .HasConversion(converter);

            modelBuilder
               .Entity<PeerIdDb>()
               .Property(e => e.Descriptor)
               .HasConversion(converter);
        }


        //protected override void OnModelCreating(ModelBuilder modelBuilder)
        //{
        //    var converter = new ValueConverter<ByteString, string>(
        //        v => v.ToBase64(),
        //        v => ByteString.FromBase64(v));

        //    //Converts//

        //    modelBuilder
        //       .Entity<PeerIdDb>()
        //       .Property(e => e.ClientId)
        //       .HasConversion(converter);

        //    modelBuilder
        //       .Entity<PeerIdDb>()
        //       .Property(e => e.ClientVersion)
        //       .HasConversion(converter);

        //    modelBuilder
        //       .Entity<PeerIdDb>()
        //       .Property(e => e.Ip)
        //       .HasConversion(converter);

        //    modelBuilder
        //       .Entity<PeerIdDb>()
        //       .Property(e => e.Descriptor)
        //       .HasConversion(converter);

        //    modelBuilder
        //       .Entity<PeerIdDb>()
        //       .Property(e => e.Port)
        //       .HasConversion(converter);

        //    modelBuilder
        //       .Entity<PeerIdDb>()
        //       .Property(e => e.PublicKey)
        //       .HasConversion(converter);


        //}
    }



    public class Contact
    {
        [Key]
        public string ContactId { get; set; }
        public string Name { get; set; }
        public string Title { get; set; }
        public int ContactTypeId { get; set; } // for partitioning on 

        public decimal SumDecimal { get; set; }

        public virtual List<EmailAddress> EmailAddresses { get; set; }
        public virtual List<PhoneNumber> PhoneNumbers { get; set; }

        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }


    public class ContactItem : Microsoft.EntityFrameworkCore.DbContext
    {
        public ContactItem() : base()
        {

        }

        [Key]
        public string ContactId { get; set; }
        public string Name { get; set; }
        public string Title { get; set; }
        public int ContactTypeId { get; set; } // for partitioning on 

        public decimal SumDecimal { get; set; }

        public virtual List<EmailAddress> EmailAddresses { get; set; }
        public virtual List<PhoneNumber> PhoneNumbers { get; set; }

        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }

    public class EmailAddress
    {
        public int EmailAddressId { get; set; }

        public string ContactId { get; set; }

        public Contact Contact { get; set; }

        public string Label { get; set; }
        public string Email { get; set; }
    }

    public class PhoneNumber
    {
        public int PhoneNumberId { get; set; }
        public int ContactId { get; set; }
        public string Label { get; set; }
        public string Number { get; set; }
    }

    public class ContactInt : ICloneable
    {
        [Key]
        public int ContactIntId { get; set; }
        public string Name { get; set; }
        public string Title { get; set; }
        public int ContactTypeId { get; set; } // for partitioning on 

        public decimal SumDecimal { get; set; }

        public virtual List<EmailAddress> EmailAddresses { get; set; }
        public virtual List<PhoneNumber> PhoneNumbers { get; set; }

        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }

    public class Node
    {
        [RepositoryPrimaryKey(Order = 1)]
        public int Id { get; set; }
        public int? ParentId { get; set; }
        public string Name { get; set; }
        public virtual Node Parent { get; private set; }
    }

    public class User
    {
        [RepositoryPrimaryKey(Order = 1)]
        public string Username { get; set; }

        [RepositoryPrimaryKey(Order = 1)]
        public int Age { get; set; }

        public string FullName { get; set; }

        public int ContactTypeId { get; set; }
    }

    public class PeerIdDb : PaperTowel
    {
        //[RepositoryPrimaryKey(Order = 1)]
        //[Key]
        //public ByteString ClientId { get; set; }
        public ByteString ClientVersion { get; set; }
        public ByteString Descriptor { get; set; }
        public ByteString Ip { get; set; }
        public ByteString Port { get; set; }
        public ByteString PublicKey { get; set; }
    }

    public abstract class PaperTowel
    {
        [RepositoryPrimaryKey(Order = 1)]
        [Key]
        //public string Id { get; set; }
        public ByteString ClientId { get; set; }

        public NetworkTemp Net { get; set; }

        public DateTime TimeStamp { get; set; }

        //public TimeSpan TimeStamp { get; set; }
    }

    public enum NetworkTemp
    {
        NETWORK_UNKNOWN = 0,
        MAINNET = 1,
        DEVNET = 2,
        TESTNET = 3
    }


    //public class PeerIdDb
    //{
    //    [RepositoryPrimaryKey(Order = 1)]
    //    [Key]
    //    public String ClientId { get; set; }
    //    public String ClientVersion { get; set; }
    //    public String Descriptor { get; set; }
    //    public byte[] Ip { get; set; }
    //    public String Port { get; set; }
    //    public ByteString PublicKey { get; set; }
    //}

}

