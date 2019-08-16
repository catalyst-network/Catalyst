using System.Security;
using Catalyst.Common.Registry;
using Catalyst.Common.Types;

namespace Catalyst.Simulator.Helpers
{
    public static class PasswordRegistryHelper
    {
        public static void AddPassword(PasswordRegistry passwordRegistry, PasswordRegistryTypes passwordRegistryTypes, string password)
        {
            var secureString = new SecureString();
            foreach (var character in password)
            {
                secureString.AppendChar(character);
            }

            passwordRegistry.AddItemToRegistry(passwordRegistryTypes, secureString);
        }
    }
}
