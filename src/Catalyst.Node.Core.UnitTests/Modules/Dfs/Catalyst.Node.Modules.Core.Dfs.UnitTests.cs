using System;
using System.IO;
using System.Linq;
using Catalyst.Node.Core.Components.Ipfs;
using FluentAssertions;
using Microsoft.CSharp.RuntimeBinder;
using Newtonsoft.Json;
using Xunit;

namespace Catalyst.Node.UnitTests.Modules.Dfs
{
    public class UT_Dfs
    {
        private static readonly IpfsConnector Ipfs = new IpfsConnector();
        private static TestDfsSettings _settings;

        private static readonly Random Random;

        private static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length).Select(s => s[Random.Next(s.Length)]).ToArray());
        }

        static UT_Dfs()
        {
            Random = new Random();
        }

        public UT_Dfs()
        {
            _settings = new TestDfsSettings {StorageType = "Ipfs", ConnectRetries = 10, IpfsVersionApi = "api/v0/"};
            Ipfs.CreateIpfsClient(_settings.IpfsVersionApi, _settings.ConnectRetries);
        }

        [Fact(Skip="We will probably use a native .Net IPFS client")]
        public void AddFile()
        {
            var tmpFile = Path.GetTempFileName();
            File.WriteAllText(tmpFile, "hello my friends");

            var hash = Ipfs.AddFile(tmpFile);
            hash.Should().Be("QmaMjZpjD17yRfCwk6Yg8aRnspyR4EcvCsqoyBECCP8bjJ");
        }

        [Fact(Skip="We will probably use a native .Net IPFS client")]
        public void AddFileAsync_EmptyFilename()
        {
            new Action(() => Ipfs.AddFile("")).Should().Throw<JsonReaderException>();
        }

        [Fact(Skip="We will probably use a native .Net IPFS client")]
        public void ReadAllTextAsync()
        {
            var text = Ipfs.ReadAllTextAsync("Qmf412jQZiuVUtdgnB36FXFX7xg5V6KEbSJ4dpQuhkLyfD");
            text.Result.Should().Be("hello world");
        }

        [Fact(Skip="We will probably use a native .Net IPFS client")]
        public void ReadAllTextAsync_Dodgy_Hash()
        {
            
            new Action(() =>{
                    var result = Ipfs.ReadAllTextAsync("Qmf412jQZiuVAbcdefghilmnopqrst12").Result;
                }).Should().Throw<AggregateException>("invalid ipfs ref path");
        }

        [Fact(Skip="We will probably use a native .Net IPFS client")]
        public void StopDoesNotThrow()
        {
            Ipfs.IsClientConnected().Should().BeTrue();

            new Action(() => Ipfs.DestroyIpfsClient())
                .Should().NotThrow();
        }

        [Fact(Skip="We will probably use a native .Net IPFS client")]
        public void Stop_Start_DoesNotThrow()
        {
            new Action(() =>
            {
                Ipfs.DestroyIpfsClient();
                Ipfs.CreateIpfsClient(_settings.IpfsVersionApi, _settings.ConnectRetries);
            }).Should().NotThrow();

            Ipfs.IsClientConnected().Should().BeTrue();
        }

        [Fact(Skip="We will probably use a native .Net IPFS client")]
        public void Stop_Add_Fail()
        {
            new Action(() =>
            {
                Ipfs.DestroyIpfsClient();

                var tmpFile = Path.GetTempFileName();
                Ipfs.AddFile(tmpFile);
            }).Should().Throw<RuntimeBinderException>("Cannot perform runtime binding on a null reference");

        }

        [Fact(Skip="We will probably use a native .Net IPFS client")]
        public void Stop_Read_Fail()
        {
            new Action(() =>
            {
                Ipfs.DestroyIpfsClient();
                var text = Ipfs.ReadAllTextAsync("Qmf412jQZiuVUtdgnB36FXFX7xg5V6KEbSJ4dpQuhkLyfD").Result; 
            }).Should().Throw<AggregateException>("Connection refused");
           
        }

        [Fact(Skip="We will probably use a native .Net IPFS client")]
        public void Stop_Twice_NoThrow()
        {
            new Action(() => {
                Ipfs.DestroyIpfsClient();
                Ipfs.DestroyIpfsClient();
            }).Should().NotThrow();
        }

        [Fact(Skip="We will probably use a native .Net IPFS client")]
        public void Start_Twice_NoThrow()
        {
            new Action(() => {
                Ipfs.CreateIpfsClient(_settings.IpfsVersionApi, _settings.ConnectRetries);
                Ipfs.CreateIpfsClient(_settings.IpfsVersionApi, _settings.ConnectRetries);
            }).Should().NotThrow();
            Ipfs.IsClientConnected().Should().BeTrue();
        }

        [Fact(Skip="We will probably use a native .Net IPFS client")]
        public void ChangeSettings_FailToStart()
        {
            new Action(() =>
            {
                var x = new TestDfsSettings {StorageType = "Ipfs", ConnectRetries = 0, IpfsVersionApi = "api/v0/"};
                Ipfs.DestroyIpfsClient();
                Ipfs.CreateIpfsClient(x.IpfsVersionApi, x.ConnectRetries);
            }).Should().Throw<InvalidOperationException>("Failed to connect with IPFS daemon");

        }

        [Fact(Skip="We will probably use a native .Net IPFS client")]
        public void SequentialAdd()
        {
            const int numIter = 50;

            for (var i = 0; i < numIter; i++)
            {
                var tmpFile = Path.GetTempFileName();
                File.WriteAllText(tmpFile, RandomString(32));

                var hash1 = Ipfs.AddFile(tmpFile);
                hash1.Length.Should().Be(46); // just make sure was added
                var hash2 = Ipfs.AddFile(tmpFile); // retrieve the hash back, does not add
                hash1.Should().Be(hash2);
            }
        }

        private class TestDfsSettings
        {
            public string StorageType { get; set; }
            public ushort ConnectRetries { get; set; }
            public string IpfsVersionApi { get; set; }
        }
    }
}