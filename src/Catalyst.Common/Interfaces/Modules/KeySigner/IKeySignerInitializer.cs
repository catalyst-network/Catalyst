using System.Security;

namespace Catalyst.Common.Interfaces.Modules.KeySigner
{
    /// <summary>
    /// Initializes the key signer
    /// </summary>
    public interface IKeySignerInitializer
    {
        /// <summary>Reads the password.</summary>
        /// <param name="keySigner">The key signer.</param>
        void ReadPassword(IKeySigner keySigner);

        /// <summary>Gets the password.</summary>
        /// <value>The password.</value>
        string Password { get; }

        /// <summary>Generates the new key.</summary>
        /// <param name="keySigner">The key signer.</param>
        void GenerateNewKey(IKeySigner keySigner);
    }
}
