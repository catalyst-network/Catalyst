#region LICENSE

/**
* Copyright (c) 2019 Catalyst Network
*
* This file is part of Catalyst.Node <https://github.com/catalyst-network/Catalyst.Node>
*
* Catalyst.Node is free software: you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation, either version 2 of the License, or
* (at your option) any later version.
*
* Catalyst.Node is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
* GNU General Public License for more details.
*
* You should have received a copy of the GNU General Public License
* along with Catalyst.Node. If not, see <https://www.gnu.org/licenses/>.
*/

#endregion

using System.Runtime.InteropServices;

namespace Catalyst.Core.Modules.Cryptography.BulletProofs
{
    public static class Ffi
    {
        private const string Library = "catalystffi";

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        private static extern int generate_key(byte[] bytes);

        internal static byte[] GeneratePrivateKey()
        {
            var key = new byte[PrivateKeyLength];
            var errorCode = generate_key(key);
            if (errorCode != 0)
            {
                Error.ThrowErrorFromErrorCode(errorCode.ToString());
            }

            return key;
        }

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        private static extern int std_sign(byte[] signature, byte[] privateKey, byte[] message, int messageLength, byte[] context, int contextLength);

        internal static byte[] StdSign(byte[] privateKey, byte[] message, byte[] context)
        {
            if (privateKey.Length != PrivateKeyLength)
            {
                Error.ThrowArgumentExceptionPrivateKeyLength(PrivateKeyLength);
            }

            var signature = new byte[SignatureLength];
            var errorCode = std_sign(signature, privateKey, message, message.Length, context, context.Length);
            if (errorCode != 0)
            {
                Error.ThrowErrorFromErrorCode(errorCode.ToString());
            }

            return signature;
        }
        
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        private static extern int std_verify(byte[] signature, byte[] publicKey, byte[] message, int messageLength, byte[] context, int contextLength, byte[] b);

        internal static bool StdVerify(byte[] signature, byte[] publicKey, byte[] message, byte[] context)
        {
            if (signature.Length != SignatureLength)
            { 
                Error.ThrowArgumentExceptionSignatureLength(SignatureLength);
            } 
            
            if (publicKey.Length != PublicKeyLength)
            { 
                Error.ThrowArgumentExceptionPublicKeyLength(PublicKeyLength);
            } 

            var isVerified = new byte[1];

            var errorCode = std_verify(signature, publicKey, message, message.Length, context, context.Length,
                isVerified);

            if (errorCode != 0)
            {
                Error.ThrowErrorFromErrorCode(errorCode.ToString());
            }

            return isVerified[0] == 1;
        }

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        private static extern int validate_public_key(byte[] publicKey);

        internal static void ValidatePublicKeyOrThrow(byte[] publicKey)
        {
            if (publicKey.Length != PublicKeyLength)
            { 
                Error.ThrowArgumentExceptionPublicKeyLength(PublicKeyLength);
            } 

            var errorCode = validate_public_key(publicKey);

            if (errorCode != 0)
            {
                Error.ThrowErrorFromErrorCode(errorCode.ToString());
            }
        }

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        private static extern void publickey_from_private(byte[] publicKey, byte[] privateKey);

        internal static byte[] GetPublicKeyFromPrivate(byte[] privateKey)
        {
            if (privateKey.Length != PrivateKeyLength)
            {
                Error.ThrowArgumentExceptionPrivateKeyLength(PrivateKeyLength);
            }

            var publicKeyBytes = new byte[PublicKeyLength];
            publickey_from_private(publicKeyBytes, privateKey);
            
            return publicKeyBytes;
        }

        internal static string GetLastError()
        {
            var errorLength = last_error_length();
            if (errorLength <= 0) return "";
            var readInto = new byte[errorLength - 1];
            return last_error_message(readInto, errorLength) > 0 ? System.Text.Encoding.UTF8.GetString(readInto) : "";
        }

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        private static extern int last_error_length();

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        private static extern int last_error_message(byte[] errorBuffer, int messageLength);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        private static extern int get_private_key_length();

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        private static extern int get_public_key_length();

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        private static extern int get_signature_length();

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        private static extern int get_max_context_length();

        private static int _privateKeyLength;

        public static int PrivateKeyLength
        {
            get
            {
                if (_privateKeyLength == 0)
                {
                    _privateKeyLength = get_private_key_length(); 
                }

                return _privateKeyLength;
            }
        }

        private static int _publicKeyLength;

        public static int PublicKeyLength
        {
            get
            {
                if (_publicKeyLength == 0)
                {
                    _publicKeyLength = get_public_key_length(); 
                }

                return _publicKeyLength;
            }
        }

        private static int _signatureLength;

        public static int SignatureLength
        {
            get
            {
                if (_signatureLength == 0)
                {
                    _signatureLength = get_signature_length(); 
                }

                return _signatureLength;
            }
        }

        private static int _signatureContextMaxLength;

        public static int SignatureContextMaxLength
        {
            get
            {
                if (_signatureContextMaxLength == 0)
                {
                    _signatureContextMaxLength = get_max_context_length(); 
                }

                return _signatureContextMaxLength;
            }
        }
    }
}
