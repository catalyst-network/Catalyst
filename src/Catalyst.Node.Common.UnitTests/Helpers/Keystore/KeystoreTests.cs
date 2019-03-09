using System;
using System.Reflection;
using Catalyst.Node.Common.Helpers.Cryptography;
using Catalyst.Node.Common.Interfaces;
using Catalyst.Node.Common.Helpers.Keystore;
using FluentAssertions;
using Serilog;
using Xunit;

namespace Catalyst.Node.Common.UnitTests.Helpers.Keystore
{
    public class KeystoreTests
    {
        private readonly IKeyStore _keystore;
        private readonly ICryptoContext _context;

        public KeystoreTests()
        {
            _context = new NSecCryptoContext();
            _keystore = new LocalKeyStore(_context, Log.Logger.ForContext(MethodBase.GetCurrentMethod().DeclaringType));
        }

        [Fact]
        public void TestStoreAndRetrieveKey()
        {
            IPrivateKey key = _context.GeneratePrivateKey();
            string address = _context.AddressFromKey(key);
            string password = "password123";
            _keystore.StoreKey(key, address, password);
            IPrivateKey retrievedKey = _keystore.GetKey(address, password);
            retrievedKey.Should().NotBeNull();
        }
        
        [Fact]
        public void TestWrongPasswordStoreAndRetrieveKey()
        {
            IPrivateKey key = _context.GeneratePrivateKey();
            string address = _context.AddressFromKey(key);
            string password = "password123";
            _keystore.StoreKey(key, address, password);

            string password2 = "incorrect password";
            Action action = () => _keystore.GetKey(address, password2);
            action.Should().Throw<Nethereum.KeyStore.Crypto.DecryptionException>("we should not be able to retrieve a key with the wrong password");
        }
    }
}