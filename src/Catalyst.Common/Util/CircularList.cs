#region LICENSE

/**
* Copyright (c) 2019 Catalyst Network
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

using System.Collections.Generic;

namespace Catalyst.Common.Util
{
    /// <summary>
    /// The circular list offers a list circular list structure
    /// No out of bounds is thrown by this list
    /// </summary>
    /// <typeparam name="T">Object type</typeparam>
    public class CircularList<T>
    {
        /// <summary>The list</summary>
        private readonly List<T> _list;

        /// <summary>The circular capacity</summary>
        private readonly int _capacity;

        /// <summary>The current position of the read/write pointer</summary>
        private int _currentPosition;

        /// <summary>Initializes a new instance of the <see cref="CircularList{T}"/> class.</summary>
        /// <param name="list">The list.</param>
        public CircularList(List<T> list) : this(list, list.Count) { }

        /// <summary>Initializes a new instance of the <see cref="CircularList{T}"/> class.</summary>
        /// <param name="list">The list.</param>
        /// <param name="capacity">The capacity.</param>
        public CircularList(List<T> list, int capacity)
        {
            _list = list;
            _capacity = capacity;
            _currentPosition = 0;
        }

        /// <summary>Sets the position.</summary>
        /// <param name="position">The position.</param>
        /// <returns></returns>
        public CircularList<T> SetPosition(int position)
        {
            _currentPosition = position;
            return this;
        }

        /// <summary>Skips the specified amount.</summary>
        /// <param name="amount">The amount.</param>
        /// <returns></returns>
        public CircularList<T> Skip(int amount)
        {
            _currentPosition += amount;
            return this;
        }

        /// <summary>Gets the specified position.</summary>
        /// <param name="position">The position.</param>
        /// <returns></returns>
        public T Get(int position) { return _list[position % _capacity]; }

        /// <summary>Takes the specified amount.</summary>
        /// <param name="amount">The amount.</param>
        /// <returns></returns>
        public T[] Take(int amount)
        {
            T[] returnValue = new T[amount];
            int limit = _currentPosition + amount;
            int returnPos = 0;

            for (int i = _currentPosition; i < limit; i++)
            {
                returnValue[returnPos] = _list[_currentPosition % _capacity];
                _currentPosition++;
                returnPos += 1;
            }

            return returnValue;
        }

        /// <summary>Adds the specified type.</summary>
        /// <param name="t">The object.</param>
        public CircularList<T> Add(T t)
        {
            int idx = _currentPosition % _capacity;
            _list[idx] = t;
            _currentPosition++;
            return this;
        }
    }
}
