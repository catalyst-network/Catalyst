using System.Security;

namespace Catalyst.Node.Common.Interfaces
{
    public interface IPasswordReader
    {
        SecureString ReadSecurePassword(string passwordContext = "Please enter your password");
    }
}