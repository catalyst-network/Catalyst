using System.Linq;
using System.Security;
using Catalyst.Node.Common.Interfaces;

namespace Catalyst.Node.Core.UnitTest.TestUtils
{
    class TestPasswordReader : IPasswordReader
    {
        public SecureString ReadSecurePassword(string passwordContext = "Please enter your password")
        {
            var secureString = new SecureString();
            "password".ToList().ForEach(c => secureString.AppendChar(c));
            secureString.MakeReadOnly();
            return secureString;
        }
    }
}
