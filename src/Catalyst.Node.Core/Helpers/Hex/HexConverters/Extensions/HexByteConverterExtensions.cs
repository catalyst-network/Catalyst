using System;
using System.Linq;
using Dawn;

namespace Catalyst.Node.Core.Helpers.Hex.HexConverters.Extensions
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
            Guard.Argument(value, nameof(value)).NotNull();
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
            Guard.Argument(value, nameof(value)).NotNull();
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
            Guard.Argument(value, nameof(value)).NotNull().NotEmpty().NotWhiteSpace();
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
            Guard.Argument(value, nameof(value)).NotNull().NotEmpty().NotWhiteSpace();
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
            Guard.Argument(first, nameof(first)).NotNull().NotEmpty().NotWhiteSpace();
            Guard.Argument(second, nameof(second)).NotNull().NotEmpty().NotWhiteSpace();
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
            Guard.Argument(value, nameof(value)).NotNull().NotEmpty().NotWhiteSpace();
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
            Guard.Argument(values, nameof(values)).NotNull()
                 .NotEmpty(_ => "Value cannot be an empty collection.");
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
            Guard.Argument(value, nameof(value)).NotNull()
                 .NotEmpty(_ => "Value cannot be an empty collection.");
            return ToHex(value).TrimStart('0');
        }

        /// <summary>
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        private static byte[] HexToByteArrayInternal(string value)
        {
            Guard.Argument(value, nameof(value)).NotNull().NotEmpty().NotWhiteSpace();

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

            var bytes = new byte[numberOfCharacters / 2]; // Initialize our byte array to hold the converted string.

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
            Guard.Argument(value, nameof(value)).NotNull().NotEmpty().NotWhiteSpace();
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
            Guard.Argument(character, nameof(character)).NotNegative().NotZero();
            Guard.Argument(index, nameof(index)).NotNegative();

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