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
using System.Text;
using Catalyst.Node.Common.Helpers.Cryptography;
using Catalyst.Node.Common.Interfaces;
using FluentAssertions;
using Xunit;

namespace Catalyst.Node.Common.UnitTests.Cryptography
{
    public class CryptographyTests
    {
        public CryptographyTests() { _context = new NSecCryptoContext(); }

        private readonly ICryptoContext _context;

        [Fact]
        public void TestFailurePrivateKeyImport()
        {
            var blob = Encoding.UTF8.GetBytes("this string is not a formatted private key");

            Action action = () => _context.ImportPrivateKey(blob);
            action.Should().Throw<FormatException>("The key BLOB is not in the correct format.");
        }

        [Fact]
        public void TestFailurePrivateKeySecondExport()
        {
            var privateKey = _context.GeneratePrivateKey();
            _context.ExportPrivateKey(privateKey);

            Action action = () => _context.ExportPrivateKey(privateKey);
            action.Should().Throw<InvalidOperationException>("The key can be exported only once.");
        }

        [Fact]
        public void TestFailurePublicKeyImport()
        {
            var blob = Encoding.UTF8.GetBytes("this string is not a formatted public key");

            Action action = () => _context.ImportPublicKey(blob);
            action.Should().Throw<FormatException>("The key BLOB is not in the correct format.");
        }

        [Fact]
        public void TestFailureSigningVerification()
        {
            var key1 = _context.GeneratePrivateKey();

            var data = Encoding.UTF8.GetBytes("Testing testing 1 2 3");
            var signature = _context.Sign(key1, data);

            var key2 = _context.GeneratePrivateKey();

            _context.Verify(key2, data, signature)
               .Should().BeFalse("signature should not verify with incorrect key");
        }

        [Fact]
        public void TestPrivateKeyExport()
        {
            var privateKey = _context.GeneratePrivateKey();

            var blob = _context.ExportPrivateKey(privateKey);
            blob.Should().NotBeNull("newly generated private key should be exportable once");
        }

        [Fact]
        public void TestPrivateKeyImportFromExported()
        {
            var privateKey = _context.GeneratePrivateKey();
            var blob = _context.ExportPrivateKey(privateKey);

            IPublicKey importedPrivateKey = _context.ImportPrivateKey(blob);
            importedPrivateKey.Should().NotBeNull("private key should be importable from a valid blob");
        }

        [Fact]
        public void TestPublicKeyFromPrivateKey()
        {
            var privateKey = _context.GeneratePrivateKey();
            var publicKey = _context.GetPublicKey(privateKey);

            publicKey.Should().NotBeNull(" a valid public key should be created from a private key");
        }

        [Fact]
        public void TestPublicKeyImport()
        {
            var privateKey = _context.GeneratePrivateKey();
            var blob = _context.ExportPublicKey(privateKey);

            var publicKey = _context.ImportPublicKey(blob);
            publicKey.Should().NotBeNull("public key should be importable from a valid blob");
        }

        [Fact]
        public void TestSigningVerification()
        {
            var privateKey = _context.GeneratePrivateKey();
            var data = Encoding.UTF8.GetBytes("Testing testing 1 2 3");
            var signature = _context.Sign(privateKey, data);

            _context.Verify(privateKey, data, signature)
               .Should().BeTrue("signature generated with private key should verify with corresponding public key");
        }

        [Fact]
        public void TestVerifyWithImportedPublicKey()
        {
            var privateKey = _context.GeneratePrivateKey();
            var data = Encoding.UTF8.GetBytes("Testing testing 1 2 3");
            var signature = _context.Sign(privateKey, data);
            var blob = _context.ExportPublicKey(privateKey);

            var importedKey = _context.ImportPublicKey(blob);
            _context.Verify(importedKey, data, signature).Should()
               .BeTrue("signature should verify with imported public key");
        }
    }
}