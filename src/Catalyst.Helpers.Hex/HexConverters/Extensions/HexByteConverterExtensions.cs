using System;
using System.Linq;

namespace Catalyst.Helpers.Hex.HexConverters.Extensions
{
    public static class HexByteConverterExtensions
    {
        private static readonly byte[] Empty = new byte[0];

        /// <summary>
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static string ToHex(this byte[] value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            return ToHex(value, false);
        }

        /// <summary>
        /// </summary>
        /// <param name="value"></param>
        /// <param name="prefix"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static string ToHex(this byte[] value, bool prefix = false)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            var strPrex = prefix ? "0x" : "";
            return strPrex + string.Concat(value.Select(b => b.ToString("x2")).ToArray());
        }

        /// <summary>
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static bool HasHexPrefix(this string value)
        {
            if (string.IsNullOrEmpty(value))
                throw new ArgumentException("Value cannot be null or empty.", nameof(value));
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(value));
            return value.StartsWith("0x");
        }

        /// <summary>
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public static string RemoveHexPrefix(this string value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(value));
            return value.Replace("0x", "");
        }

        /// <summary>
        /// </summary>
        /// <param name="first"></param>
        /// <param name="second"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public static bool IsTheSameHex(this string first, string second)
        {
            if (first == null) throw new ArgumentNullException(nameof(first));
            if (second == null) throw new ArgumentNullException(nameof(second));
            if (string.IsNullOrEmpty(first))
                throw new ArgumentException("Value cannot be null or empty.", nameof(first));
            if (string.IsNullOrEmpty(second))
                throw new ArgumentException("Value cannot be null or empty.", nameof(second));
            if (string.IsNullOrWhiteSpace(first))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(first));
            if (string.IsNullOrWhiteSpace(second))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(second));
            return string.Equals(EnsureHexPrefix(first).ToLower(), EnsureHexPrefix(second).ToLower(),
                StringComparison.Ordinal);
        }

        /// <summary>
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static string EnsureHexPrefix(this string value)
        {
            if (string.IsNullOrEmpty(value))
                throw new ArgumentException("Value cannot be null or empty.", nameof(value));
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(value));
            if (!value.HasHexPrefix())
                return "0x" + value;
            return value;
        }

        /// <summary>
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public static string[] EnsureHexPrefix(this string[] values)
        {
            if (values == null) throw new ArgumentNullException(nameof(values));
            if (values.Length == 0) throw new ArgumentException("Value cannot be an empty collection.", nameof(values));
            foreach (var value in values)
                value.EnsureHexPrefix();
            return values;
        }

        /// <summary>
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public static string ToHexCompact(this byte[] value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            if (value.Length == 0) throw new ArgumentException("Value cannot be an empty collection.", nameof(value));
            return ToHex(value).TrimStart('0');
        }

        /// <summary>
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        private static byte[] HexToByteArrayInternal(string value)
        {
            if (string.IsNullOrEmpty(value))
                throw new ArgumentException("Value cannot be null or empty.", nameof(value));
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(value));
            byte[] bytes;
            if (string.IsNullOrEmpty(value))
            {
                bytes = Empty;
            }
            else
            {
                var stringLength = value.Length;
                var characterIndex = value.StartsWith("0x", StringComparison.Ordinal) ? 2 : 0;
                // Does the string define leading HEX indicator '0x'. Adjust starting index accordingly.               
                var numberOfCharacters = stringLength - characterIndex;

                var addLeadingZero = false;
                if (0 != numberOfCharacters % 2)
                {
                    addLeadingZero = true;

                    numberOfCharacters += 1; // Leading '0' has been striped from the string presentation.
                }

                bytes = new byte[numberOfCharacters / 2]; // Initialize our byte array to hold the converted string.

                var writeIndex = 0;
                if (addLeadingZero)
                {
                    bytes[writeIndex++] = FromCharacterToByte(value[characterIndex], characterIndex);
                    characterIndex += 1;
                }

                for (var readIndex = characterIndex; readIndex < value.Length; readIndex += 2)
                {
                    var upper = FromCharacterToByte(value[readIndex], readIndex, 4);
                    var lower = FromCharacterToByte(value[readIndex + 1], readIndex + 1);

                    bytes[writeIndex++] = (byte) (upper | lower);
                }
            }

            return bytes;
        }

        /// <summary>
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="FormatException"></exception>
        public static byte[] HexToByteArray(this string value)
        {
            if (string.IsNullOrEmpty(value))
                throw new ArgumentException("Value cannot be null or empty.", nameof(value));
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(value));
            try
            {
                return HexToByteArrayInternal(value);
            }
            catch (FormatException ex)
            {
                throw new FormatException($"String '{value}' could not be converted to byte array (not hex?).", ex);
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="character"></param>
        /// <param name="index"></param>
        /// <param name="shift"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <exception cref="FormatException"></exception>
        private static byte FromCharacterToByte(char character, int index, int shift = 0)
        {
            if (character <= 0) throw new ArgumentOutOfRangeException(nameof(character));
            if (index < 0) throw new ArgumentOutOfRangeException(nameof(index));
            var value = (byte) character;
            if (0x40 < value && 0x47 > value || 0x60 < value && 0x67 > value)
            {
                if (0x40 == (0x40 & value))
                    if (0x20 == (0x20 & value))
                        value = (byte) ((value + 0xA - 0x61) << shift);
                    else
                        value = (byte) ((value + 0xA - 0x41) << shift);
            }
            else if (0x29 < value && 0x40 > value)
            {
                value = (byte) ((value - 0x30) << shift);
            }
            else
            {
                throw new FormatException(
                    $"Character '{character}' at index '{index}' is not valid alphanumeric character.");
            }

            return value;
        }
    }
}