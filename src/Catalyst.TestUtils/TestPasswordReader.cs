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

using System.Linq;
using System.Security;
using Catalyst.Abstractions.Cryptography;

namespace Catalyst.TestUtils
{
    public sealed class TestPasswordReader : IPasswordReader
    {
        private readonly string _expectedPassword;

        public TestPasswordReader(string expectedPassword = "password")
        {
            _expectedPassword = expectedPassword;
        }

        public static SecureString BuildSecureStringPassword(string expectedPassword)
        {
            var secureString = new SecureString();
            expectedPassword.ToList().ForEach(c => secureString.AppendChar(c));
            secureString.MakeReadOnly();
            return secureString;
        }

        public SecureString ReadSecurePassword(string prompt = "Please enter your password")
        {
            return BuildSecureStringPassword(_expectedPassword);
        }
    }
}
