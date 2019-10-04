using System;
using Autofac;
using Catalyst.Abstractions.Hashing;
using FluentAssertions;
using Ipfs.Registry;
using Xunit;

namespace Catalyst.Core.Modules.Hashing.Tests.UnitTests
{
    public class HashingTests
    {
        private readonly IContainer _container;

        public HashingTests()
        {
            var builder = new ContainerBuilder();
            builder.RegisterModule<HashingModule>();

            _container = builder.Build();
            _container.BeginLifetimeScope();
        }

        [Fact]
        public void HashProvider_Can_Be_Resolved() { _container.Resolve<IHashProvider>().Should().NotBeNull(); }

        [Fact]
        public void MultihashAlgorithm_Can_Be_Resolved()
        {
            _container.Resolve<HashingAlgorithm>().Should().NotBeNull();
        }

        [Fact]
        public void Can_Hash_Data()
        {
            var hashProvider = _container.Resolve<IHashProvider>();
            var data = BitConverter.GetBytes(0xDEADBEEF);
            var multiHash = hashProvider.ComputeMultiHash(data);
            multiHash.Should().NotBeNull();
        }
    }
}
