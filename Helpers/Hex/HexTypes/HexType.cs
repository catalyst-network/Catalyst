using System;
using ADL.Hex.HexConvertors;
using ADL.Hex.HexConvertors.Extensions;

namespace ADL.Hex.HexTypes
{
    public class HexRPCType<T>
    {
        protected IHexConvertor<T> convertor;

        protected string hexValue;

        protected T value;

        protected object lockingObject = new object();
        protected bool needsInitialisingValue;

        protected T GetValue()
        {
            lock (lockingObject)
            {
                if (needsInitialisingValue)
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

        public string HexValue
        {
            get => hexValue;
            set => SetHexAndFlagValueToBeInitialised(value);
        }

        public T Value
        {
            get => GetValue();
            set => InitialiseFromValue(value);
        }

        protected void SetHexAndFlagValueToBeInitialised(string newHexValue)
        {
            hexValue = newHexValue.EnsureHexPrefix();
            lock (lockingObject)
            {
                needsInitialisingValue = true;
            }
        }

        protected void InitialiseValueFromHex(string newHexValue)
        {
            value = ConvertFromHex(newHexValue);
        }

        protected void InitialiseFromValue(T newValue)
        {
            hexValue = ConvertToHex(newValue).EnsureHexPrefix();
            value = newValue;
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
            return HexValue.HexToByteArray();
        }

        public static implicit operator byte[](HexRPCType<T> hexRpcType)
        {
            return hexRpcType.ToHexByteArray();
        }

        public static implicit operator T(HexRPCType<T> hexRpcType)
        {
            return hexRpcType.Value;
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }
    }
}