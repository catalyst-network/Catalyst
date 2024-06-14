using System;
using Nethereum.Hex.HexConvertors;
using Nethereum.Hex.HexConvertors.Extensions;

namespace Nethereum.Hex.HexTypes
{
    public class HexRPCType<T>: IEquatable<HexRPCType<T>>
    {
        protected IHexConvertor<T> convertor;

        protected string? hexValue;

        protected T? value;

        protected object lockingObject = new();
        protected bool needsInitialisingValue;

        protected T? GetValue()
        {
            lock (lockingObject)
            {
                if (needsInitialisingValue &&
                    !string.IsNullOrEmpty(hexValue))
                {
                    InitialiseValueFromHex(hexValue);
                    needsInitialisingValue = false;
                }
                return value;
            }
        }
        protected HexRPCType(IHexConvertor<T> convertor)
        {
            this.convertor = convertor;
        }

        public HexRPCType(IHexConvertor<T> convertor, string hexValue)
        {
            this.convertor = convertor;
            SetHexAndFlagValueToBeInitialised(hexValue);
        }

        public HexRPCType(T value, IHexConvertor<T> convertor)
        {
            this.convertor = convertor;
            InitialiseFromValue(value);
        }

        public string? HexValue
        {
            get => hexValue;
            set => SetHexAndFlagValueToBeInitialised(value);
        }

        public T? Value
        {
            get => GetValue();
            set => InitialiseFromValue(value);
        }

        protected void SetHexAndFlagValueToBeInitialised(string? newHexValue)
        {
            if (!string.IsNullOrEmpty(newHexValue))
            {
                hexValue = newHexValue.EnsureHexPrefix();
                lock (lockingObject)
                {
                    needsInitialisingValue = true;
                }
            }
        }

        protected void InitialiseValueFromHex(string newHexValue)
        {
            value = ConvertFromHex(newHexValue);
        }

        protected void InitialiseFromValue(T? newValue)
        {
            if (newValue != null)
            {
                hexValue = ConvertToHex(newValue).EnsureHexPrefix();
                value = newValue;
            }
        }

        protected string ConvertToHex(T newValue)
        {
            return convertor.ConvertToHex(newValue);
        }

        protected T ConvertFromHex(string newHexValue)
        {
            return convertor.ConvertFromHex(newHexValue);
        }

        public byte[] ToHexByteArray()
        {
            if (HexValue != null)
            {
                return HexValue.HexToByteArray();
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        public static implicit operator byte[](HexRPCType<T> hexRpcType)
        {
            return hexRpcType.ToHexByteArray();
        }

        public static implicit operator T?(HexRPCType<T> hexRpcType)
        {
            return hexRpcType.Value;
        }

        public static bool operator == (HexRPCType<T> lhs, HexRPCType<T> rhs)
        {
            var lhso = (object) lhs;
            var rhso = (object) rhs;
            if (lhso != null) return lhso.Equals(rhso);
            if (rhso != null) return false; //lhs is null / rhs is not
            return true; // both null;
        }
        public static bool operator !=(HexRPCType<T> lhs, HexRPCType<T> rhs)
        {
            return !(lhs == rhs);
        }

        public override int GetHashCode()
        {
            return Value != null ? Value.GetHashCode() : 0;
        }

        public override bool Equals(object? obj)
        {
            if (obj is HexRPCType<T> val && val.Value is not null)
            {
                // Value is lazy loaded and always e
                return val.Value.Equals(Value);
            }

            return false;
        }

        public bool Equals(HexRPCType<T>? other)
        {
            if (other is not null)
            {
                return this.Equals((object)other);
            }
            else
            {
                return false;
            }
        }
    }
}
