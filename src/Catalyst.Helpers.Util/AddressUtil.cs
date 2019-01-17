using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Catalyst.Helpers.Hex.HexConverters.Extensions;

namespace Catalyst.Helpers.Util
{
    public class AddressUtil
    {
        private static AddressUtil _current;
        public static AddressUtil Current => _current ?? (_current = new AddressUtil());

        /// <summary>
        /// 
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public string ConvertToChecksumAddress(string address)
        {
            if (address == null) throw new ArgumentNullException(nameof(address));
            if (string.IsNullOrEmpty(address))
                throw new ArgumentException("Value cannot be null or empty.", nameof(address));
            if (string.IsNullOrWhiteSpace(address))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(address));
            address = address.ToLower().RemoveHexPrefix();
            var addressHash = new Sha3Keccack().CalculateHash(address);
            var checksumAddress = "0x";

            for (var i = 0; i < address.Length; i++)
                if (int.Parse(addressHash[i].ToString(), NumberStyles.HexNumber) > 7)
                    checksumAddress += address[i].ToString().ToUpper();
                else
                    checksumAddress += address[i];
            return checksumAddress;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public string ConvertToValid20ByteAddress(string address)
        {
            if (address == null) throw new ArgumentNullException(nameof(address));
            if (string.IsNullOrEmpty(address))
                throw new ArgumentException("Value cannot be null or empty.", nameof(address));
            if (string.IsNullOrWhiteSpace(address))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(address));
            address = address.RemoveHexPrefix();
            return address.PadLeft(40, '0').EnsureHexPrefix();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public bool IsValidAddressLength(string address)
        {
            if (address == null) throw new ArgumentNullException(nameof(address));
            if (string.IsNullOrEmpty(address))
                throw new ArgumentException("Value cannot be null or empty.", nameof(address));
            if (string.IsNullOrWhiteSpace(address))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(address));
            address = address.RemoveHexPrefix();
            return address.Length == 40;
        }

        /// <summary>
        /// Validates if the hex string is 40 alphanumeric characters
        /// </summary>
        public bool IsValidCatalystAddressHexFormat(string address)
        {
            if (address == null) throw new ArgumentNullException(nameof(address));
            if (string.IsNullOrEmpty(address))
                throw new ArgumentException("Value cannot be null or empty.", nameof(address));
            if (string.IsNullOrWhiteSpace(address))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(address));
            return address.HasHexPrefix() && IsValidAddressLength(address) &&
                   address.ToCharArray().All(char.IsLetterOrDigit);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public bool IsChecksumAddress(string address)
        {
            if (address == null) throw new ArgumentNullException(nameof(address));
            if (string.IsNullOrEmpty(address))
                throw new ArgumentException("Value cannot be null or empty.", nameof(address));
            if (string.IsNullOrWhiteSpace(address))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(address));
            address = address.RemoveHexPrefix();
            var addressHash = new Sha3Keccack().CalculateHash(address.ToLower());

            for (var i = 0; i < 40; i++)
            {
                var value = int.Parse(addressHash[i].ToString(), NumberStyles.HexNumber);
                // the nth letter should be uppercase if the nth digit of casemap is 1
                if (value > 7 && address[i].ToString().ToUpper() != address[i].ToString() ||
                    value <= 7 && address[i].ToString().ToLower() != address[i].ToString())
                    return false;
            }
            return true;
        }
    }
}
