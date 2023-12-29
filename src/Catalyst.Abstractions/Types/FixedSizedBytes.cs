#region LICENSE

/**
* Copyright (c) 2024 Catalyst Network
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

using System;

namespace Catalyst.Abstractions.Types
{
    public abstract class FixedSizeBytes<T> where T : FixedSizeBytes<T>, new()
    {
        protected abstract int Size { get; }

        private byte[] _rawBytes;

        protected byte[] RawBytes
        {
            get => _rawBytes ?? new byte[Size];
            set
            { 
                if (value?.Length == Size)
                {
                    _rawBytes = value;
                }
                else
                {
                    throw new ArgumentException($"The array should have length {Size.ToString()}");
                }
            }
        }
        
        public static T RandomBytes()
        {
            var newBytes = new T();
            var random = new Random();
            random.NextBytes(newBytes.RawBytes);
            return newBytes;
        }
    }   
}
