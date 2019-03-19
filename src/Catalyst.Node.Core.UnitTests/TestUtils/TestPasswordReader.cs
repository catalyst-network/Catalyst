using System.Linq;
using System.Security;
using Catalyst.Node.Common.Interfaces;

namespace Catalyst.Node.Core.UnitTest.TestUtils
{
    class TestPasswordReader : IPasswordReader
    {
        private const string Password = "password";

        public SecureString ReadSecurePassword(string passwordContext = "Please enter your password")
        {
            var secureString = new SecureString();
            Password.ToList().ForEach(c => secureString.AppendChar(c));
            secureString.MakeReadOnly();
            return secureString;
        }

        public char[] ReadSecurePasswordAsChars(string passwordContext = "Please enter your password")
        {
            return Password.ToCharArray();
        }
    }
}
