﻿using System;
using System.Numerics;

namespace Catalyst.Helpers.Math
{
    public struct BigDecimal
    {
        public BigInteger Value { get; }

        public byte Decimals { get; }

        public int Sign => Value.Sign;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="decimals"></param>
        public BigDecimal(BigInteger value, byte decimals)
        {
            Value = value;
            Decimals = decimals;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="decimals"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public BigDecimal ChangeDecimals(byte decimals)
        {
            if (Decimals == decimals) return this;
            BigInteger value;
            if (Decimals < decimals)
            {
                value = Value * BigInteger.Pow(10, decimals - Decimals);
            }
            else
            {
                var divisor = BigInteger.Pow(10, Decimals - decimals);
                value = BigInteger.DivRem(Value, divisor, out var remainder);
                if (remainder > BigInteger.Zero)
                    throw new ArgumentOutOfRangeException();
            }

            return new BigDecimal(value, decimals);
        }

        public static BigDecimal Parse(string s, byte decimals)
        {
            if (!TryParse(s, decimals, out var result))
                throw new FormatException();
            return result;
        }

        public Fixed8 ToFixed8()
        {
            try
            {
                return new Fixed8((long) ChangeDecimals(8).Value);
            }
            catch (Exception ex)
            {
                throw new InvalidCastException(ex.Message, ex);
            }
        }

        public override string ToString()
        {
            var divisor = BigInteger.Pow(10, Decimals);
            var result = BigInteger.DivRem(Value, divisor, out var remainder);
            if (remainder == 0) return result.ToString();
            return $"{result}.{remainder.ToString("d" + Decimals)}".TrimEnd('0');
        }

        public static bool TryParse(string s, byte decimals, out BigDecimal result)
        {
            var e = 0;
            var index = s.IndexOfAny(new[] {'e', 'E'});
            if (index >= 0)
            {
                if (!sbyte.TryParse(s.Substring(index + 1), out var e_temp))
                {
                    result = default(BigDecimal);
                    return false;
                }

                e = e_temp;
                s = s.Substring(0, index);
            }

            index = s.IndexOf('.');
            if (index >= 0)
            {
                s = s.TrimEnd('0');
                e -= s.Length - index - 1;
                s = s.Remove(index, 1);
            }

            var ds = e + decimals;
            if (ds < 0)
            {
                result = default(BigDecimal);
                return false;
            }

            if (ds > 0)
                s += new string('0', ds);
            if (!BigInteger.TryParse(s, out var value))
            {
                result = default(BigDecimal);
                return false;
            }

            result = new BigDecimal(value, decimals);
            return true;
        }
    }
}