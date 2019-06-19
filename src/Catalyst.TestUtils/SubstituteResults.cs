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

using System;
using System.Collections.Generic;

namespace Catalyst.TestUtils
{
    //nice idea from https://stackoverflow.com/questions/12163840/nsubstitute-multiple-return-sequence
    public class SubstituteResults<T>
    {
        private readonly Queue<Func<T>> _values = new Queue<Func<T>>();
        public SubstituteResults(T result) { _values.Enqueue(() => result); }
        public SubstituteResults(Func<T> value) { _values.Enqueue(value); }
        public SubstituteResults<T> Then(T value) { return Then(() => value); }

        public SubstituteResults<T> Then(Func<T> value)
        {
            _values.Enqueue(value);
            return this;
        }

        public T Next() { return _values.Dequeue()(); }
    }
}
