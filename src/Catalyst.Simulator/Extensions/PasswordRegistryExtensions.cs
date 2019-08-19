using Catalyst.Common.Registry;
using Catalyst.Common.Types;
using Catalyst.Simulator.Helpers;

namespace Catalyst.Simulator.Extensions
{
    public static class PasswordRegistryExtensions
    {
        public static PasswordRegistry SetFromOptions(this PasswordRegistry passwordRegistry, Options options)
        {
            if (!string.IsNullOrEmpty(options.NodePassword))
            {
                PasswordRegistryHelper.AddPassword(passwordRegistry, PasswordRegistryTypes.DefaultNodePassword,
                    options.NodePassword);
            }

            if (!string.IsNullOrEmpty(options.SslCertPassword))
            {
                PasswordRegistryHelper.AddPassword(passwordRegistry, PasswordRegistryTypes.CertificatePassword,
                    options.SslCertPassword);
            }

            return passwordRegistry;
        }
    }
}
