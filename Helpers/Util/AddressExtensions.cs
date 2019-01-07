namespace ADL.Util
{
    public static class AddressExtensions
    {
        public static string ConvertToAtlasereumChecksumAddress(this string address)
        {
           return AddressUtil.Current.ConvertToChecksumAddress(address);
        }

        public static bool IsAtlasereumChecksumAddress(this string address)
        {
            return AddressUtil.Current.IsChecksumAddress(address);
        }

        /// <summary>
        /// Validates if the hex string is 40 alphanumeric characters
        /// </summary>
        public static bool IsValidAtlasereumAddressHexFormat(this string address)
        {
            return AddressUtil.Current.IsValidAtlasereumAddressHexFormat(address);
        }

        public static bool IsValidAtlasereumAddressLength(this string address)
        {
            return AddressUtil.Current.IsValidAddressLength(address);
        }
    }
}