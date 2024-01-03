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

using System.Security;
using Catalyst.Core.Lib.Cryptography;
using NUnit.Framework;

namespace Catalyst.Core.Lib.Tests.UnitTests.Cryptography
{
    public class SecureStringExtensionsTest
    {
        [Test]
        public void UseBytes()
        {
            var secret = new SecureString();
            var expected = new[]
            {
                'a', 'b', 'c'
            };
            
            foreach (var c in expected)
            {
                secret.AppendChar(c);
            }
            
            secret.UseSecretBytes(bytes =>
            {
                Assert.Equals(expected.Length, bytes.Length);
                for (var i = 0; i < expected.Length; ++i)
                {
                    Assert.Equals(expected[i], (int) bytes[i]);
                }
            });
        }
    }
}
