using System.Security;

namespace Catalyst.Node.Core.Helpers.Cryptography {
    public interface IPasswordReader
    {
        SecureString ReadSecurePassword(string passwordContext = "Please enter your password");
    }
}