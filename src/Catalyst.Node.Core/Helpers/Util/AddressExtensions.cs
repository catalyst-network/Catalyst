using System;

namespace Catalyst.Node.Core.Helpers.Util
{
    public static class AddressExtensions
    {
        /// <summary>
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public static string ConvertToCatalystChecksumAddress(this string address)
        {
            if (address == null) throw new ArgumentNullException(nameof(address));
            if (string.IsNullOrEmpty(address))
                throw new ArgumentException("Value cannot be null or empty.", nameof(address));
            if (string.IsNullOrWhiteSpace(address))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(address));
            return AddressUtil.Current.ConvertToChecksumAddress(address);
        }

        /// <summary>
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static bool IsCatalystChecksumAddress(this string address)
        {
            if (address == null) throw new ArgumentNullException(nameof(address));
            if (string.IsNullOrEmpty(address))
                throw new ArgumentException("Value cannot be null or empty.", nameof(address));
            if (string.IsNullOrWhiteSpace(address))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(address));
            return AddressUtil.Current.IsChecksumAddress(address);
        }

        /// <summary>
        ///     Validates if the hex string is 40 alphanumeric characters
        /// </summary>
        public static bool IsValidCatalystAddressHexFormat(this string address)
        {
            if (address == null) throw new ArgumentNullException(nameof(address));
            if (string.IsNullOrEmpty(address))
                throw new ArgumentException("Value cannot be null or empty.", nameof(address));
            if (string.IsNullOrWhiteSpace(address))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(address));
            return AddressUtil.Current.IsValidCatalystAddressHexFormat(address);
        }

        /// <summary>
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static bool IsValidCatalystAddressLength(this string address)
        {
            if (address == null) throw new ArgumentNullException(nameof(address));
            if (string.IsNullOrEmpty(address))
                throw new ArgumentException("Value cannot be null or empty.", nameof(address));
            if (string.IsNullOrWhiteSpace(address))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(address));
            return AddressUtil.Current.IsValidAddressLength(address);
        }
    }
}