using System.Security;

namespace Catalyst.Node.Common.Helpers.Cryptography {
    public interface IPasswordReader
    {
        SecureString ReadSecurePassword(string passwordContext = "Please enter your password");
    }
}