using System.Security;

namespace Catalyst.Node.Common.Cryptography {
    public interface IPasswordReader
    {
        SecureString ReadSecurePassword(string passwordContext = "Please enter your password");
    }
}