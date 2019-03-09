/*
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