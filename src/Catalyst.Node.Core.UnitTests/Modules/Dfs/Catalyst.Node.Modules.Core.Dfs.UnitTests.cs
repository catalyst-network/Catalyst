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
        private static readonly IpfsConnector _ipfs = new IpfsConnector();
        private static TestDfsSettings _settings;

        private static readonly Random random = new Random();

        private static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray());
        }

        static UT_Dfs()
        {
            random = new Random();
        }

        public UT_Dfs()
        {
            _settings = new TestDfsSettings {StorageType = "Ipfs", ConnectRetries = 10, IpfsVersionApi = "api/v0/"};
            _ipfs.CreateIpfsClient(_settings.IpfsVersionApi, _settings.ConnectRetries);
        }

        [Fact]
        public void AddFile()
        {
            var tmpFile = Path.GetTempFileName();
            File.WriteAllText(tmpFile, "hello my friends");

            var hash = _ipfs.AddFile(tmpFile);
            hash.Should().Be("QmaMjZpjD17yRfCwk6Yg8aRnspyR4EcvCsqoyBECCP8bjJ");
        }

        [Fact]
        public void AddFileAsync_EmptyFilename()
        {
            new Action(() => _ipfs.AddFile("")).Should().Throw<JsonReaderException>();
        }

        [Fact]
        public void ReadAllTextAsync()
        {
            var text = _ipfs.ReadAllTextAsync("Qmf412jQZiuVUtdgnB36FXFX7xg5V6KEbSJ4dpQuhkLyfD");
            text.Result.Should().Be("hello world");
        }

        [Fact]
        public void ReadAllTextAsync_Dodgy_Hash()
        {
            
            new Action(() =>{
                    var result = _ipfs.ReadAllTextAsync("Qmf412jQZiuVAbcdefghilmnopqrst12").Result;
                }).Should().Throw<AggregateException>("invalid ipfs ref path");
        }

        [Fact]
        public void StopDoesNotThrow()
        {
            _ipfs.IsClientConnected().Should().BeTrue();

            new Action(() => _ipfs.DestroyIpfsClient())
                .Should().NotThrow();
        }

        [Fact]
        public void Stop_Start_DoesNotThrow()
        {
            new Action(() =>
            {
                _ipfs.DestroyIpfsClient();
                _ipfs.CreateIpfsClient(_settings.IpfsVersionApi, _settings.ConnectRetries);
            }).Should().NotThrow();

            _ipfs.IsClientConnected().Should().BeTrue();
        }

        [Fact]
        public void Stop_Add_Fail()
        {
            new Action(() =>
            {
                _ipfs.DestroyIpfsClient();

                var tmpFile = Path.GetTempFileName();
                _ipfs.AddFile(tmpFile);
            }).Should().Throw<RuntimeBinderException>("Cannot perform runtime binding on a null reference");

        }

        [Fact]
        public void Stop_Read_Fail()
        {
            new Action(() =>
            {
                _ipfs.DestroyIpfsClient();
                var text = _ipfs.ReadAllTextAsync("Qmf412jQZiuVUtdgnB36FXFX7xg5V6KEbSJ4dpQuhkLyfD").Result; 
            }).Should().Throw<AggregateException>("Connection refused");
           
        }

        [Fact]
        public void Stop_Twice_NoThrow()
        {
            new Action(() => {
                _ipfs.DestroyIpfsClient();
                _ipfs.DestroyIpfsClient();
            }).Should().NotThrow();
        }

        [Fact]
        public void Start_Twice_NoThrow()
        {
            new Action(() => {
                _ipfs.CreateIpfsClient(_settings.IpfsVersionApi, _settings.ConnectRetries);
                _ipfs.CreateIpfsClient(_settings.IpfsVersionApi, _settings.ConnectRetries);
            }).Should().NotThrow();
            _ipfs.IsClientConnected().Should().BeTrue();
        }

        [Fact]
        public void ChangeSettings_FailToStart()
        {
            new Action(() =>
            {
                var x = new TestDfsSettings {StorageType = "Ipfs", ConnectRetries = 0, IpfsVersionApi = "api/v0/"};
                _ipfs.DestroyIpfsClient();
                _ipfs.CreateIpfsClient(x.IpfsVersionApi, x.ConnectRetries);
            }).Should().Throw<InvalidOperationException>("Failed to connect with IPFS daemon");

        }

        [Fact]
        public void SequentialAdd()
        {
            const int numIter = 50;

            for (var i = 0; i < numIter; i++)
            {
                var tmpFile = Path.GetTempFileName();
                File.WriteAllText(tmpFile, RandomString(32));

                var hash1 = _ipfs.AddFile(tmpFile);
                hash1.Length.Should().Be(46); // just make sure was added
                var hash2 = _ipfs.AddFile(tmpFile); // retrieve the hash back, does not add
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