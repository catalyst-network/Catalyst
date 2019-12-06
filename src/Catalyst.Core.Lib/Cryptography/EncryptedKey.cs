﻿namespace Catalyst.Core.Lib.Cryptography
{
    /// <summary>
    ///   A private key that is password protected.
    /// </summary>
    public class EncryptedKey
    {
        /// <summary>
        ///   The local name of the key.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///   The unique ID of the key.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        ///   PKCS #8 container.
        /// </summary>
        /// <value>
        ///   Password protected PKCS #8 structure in the PEM formatw
        /// </value>
        public string Pem { get; set; }
    }
}
