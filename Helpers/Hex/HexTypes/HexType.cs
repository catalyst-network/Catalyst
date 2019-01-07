using System;
using ADL.Hex.HexConverters;
using ADL.Hex.HexConverters.Extensions;

namespace ADL.Hex.HexTypes
{
    public class HexRpcType<T>
    {
        private T value;
        private string hexValue;
        private bool NeedsInitialisingValue;
        private readonly IHexConvertor<T> converter;
        private readonly object LockingObject = new object(); //@TODO is it wise to share locking object?

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private T GetValue()
        {
            lock (LockingObject)
            {
                if (NeedsInitialisingValue)
                {
                    InitialiseValueFromHex(hexValue);
                    NeedsInitialisingValue = false;
                }
                return value;
            }
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="converter"></param>
        /// <exception cref="ArgumentNullException"></exception>
        protected HexRpcType(IHexConvertor<T> converter)
        {
            if (converter == null) throw new ArgumentNullException(nameof(converter));
            this.converter = converter;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="converter"></param>
        /// <param name="hexValue"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        protected HexRpcType(IHexConvertor<T> converter, string hexValue)
        {
            if (hexValue == null) throw new ArgumentNullException(nameof(hexValue));
            if (converter == null) throw new ArgumentNullException(nameof(converter));
            if (converter == null) throw new ArgumentNullException(nameof(converter));
            if (string.IsNullOrEmpty(hexValue))
                throw new ArgumentException("Value cannot be null or empty.", nameof(hexValue));
            if (converter == null) throw new ArgumentNullException(nameof(converter));
            if (string.IsNullOrWhiteSpace(hexValue))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(hexValue));

            this.converter = converter;
            SetHexAndFlagValueToBeInitialised(hexValue);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="converter"></param>
        /// <exception cref="ArgumentNullException"></exception>
        protected HexRpcType(T value, IHexConvertor<T> converter)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            if (converter == null) throw new ArgumentNullException(nameof(converter));

            this.converter = converter;
            InitialiseFromValue(value);
        }

        public string HexValue
        {
            get => hexValue;
            protected set => SetHexAndFlagValueToBeInitialised(value);
        }

        protected T Value
        {
            get => GetValue();
            set => InitialiseFromValue(value);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="newHexValue"></param>
        /// <exception cref="ArgumentException"></exception>
        private void SetHexAndFlagValueToBeInitialised(string newHexValue)
        {
            if (string.IsNullOrEmpty(newHexValue))
                throw new ArgumentException("Value cannot be null or empty.", nameof(newHexValue));
            if (string.IsNullOrWhiteSpace(newHexValue))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(newHexValue));
            hexValue = newHexValue.EnsureHexPrefix();
            lock (LockingObject)
            {
                NeedsInitialisingValue = true;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="newHexValue"></param>
        /// <exception cref="ArgumentException"></exception>
        private void InitialiseValueFromHex(string newHexValue)
        {
            if (string.IsNullOrEmpty(newHexValue))
                throw new ArgumentException("Value cannot be null or empty.", nameof(newHexValue));
            if (string.IsNullOrWhiteSpace(newHexValue))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(newHexValue));
            value = ConvertFromHex(newHexValue);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="newValue"></param>
        /// <exception cref="ArgumentNullException"></exception>
        private void InitialiseFromValue(T newValue)
        {
            if (newValue == null) throw new ArgumentNullException(nameof(newValue));
            hexValue = ConvertToHex(newValue).EnsureHexPrefix();
            lock (LockingObject)
            {
                value = newValue;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="newValue"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        private string ConvertToHex(T newValue)
        {
            if (newValue == null) throw new ArgumentNullException(nameof(newValue));
            lock (LockingObject)
            {
                return converter.ConvertToHex(newValue);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="newHexValue"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        private T ConvertFromHex(string newHexValue)
        {
            if (string.IsNullOrEmpty(newHexValue))
                throw new ArgumentException("Value cannot be null or empty.", nameof(newHexValue));
            if (string.IsNullOrWhiteSpace(newHexValue))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(newHexValue));
            return converter.ConvertFromHex(newHexValue);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private byte[] ToHexByteArray()
        {
            return HexValue.HexToByteArray();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hexRpcType"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static implicit operator byte[](HexRpcType<T> hexRpcType)
        {
            if (hexRpcType == null) throw new ArgumentNullException(nameof(hexRpcType));
            return hexRpcType.ToHexByteArray();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hexRpcType"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static implicit operator T(HexRpcType<T> hexRpcType)
        {
            if (hexRpcType == null) throw new ArgumentNullException(nameof(hexRpcType));
            return hexRpcType.Value;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }
    }
}
