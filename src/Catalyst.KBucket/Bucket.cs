#region LICENSE

/**
* Copyright (c) 2022 Catalyst Network
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
using System.Linq;

namespace Catalyst.KBucket
{
    /// <summary>
    ///   A binary tree node in the <see>
    ///       <cref>Catalyst.KBucket{T}</cref>
    ///   </see>
    ///   .
    /// </summary>
    public sealed class Bucket<T>
        where T : class, IContact
    {
        /// <summary>
        ///   The contacts in the bucket.
        /// </summary>
        public List<T> Contacts = new ();

        /// <summary>
        ///  Determines if the bucket can be split.
        /// </summary>
        public bool DontSplit;

        /// <summary>
        ///   The left hand node.
        /// </summary>
        public Bucket<T> Left;

        /// <summary>
        ///   The right hand node.
        /// </summary>
        public Bucket<T> Right;

        /// <summary>
        ///   Determines if the <see cref="Contacts"/> contains the item.
        /// </summary>
        public bool Contains(T item) { return Contacts != null && Contacts.Any(c => c.Id.SequenceEqual(item.Id)); }

        /// <summary>
        ///   Gets the first contact with the ID.
        /// </summary>
        /// <param name="id"></param>
        /// <returns>
        ///   The matching contact or <b>null</b>.
        /// </returns>
        public T Get(IEnumerable<byte> id) { return Contacts?.FirstOrDefault(c => c.Id.SequenceEqual(id)); }

        internal int IndexOf(byte[] id)
        {
            if (Contacts == null)
            {
                return -1;
            }

            return Contacts.FindIndex(c => c.Id.SequenceEqual(id));
        }

        internal int DeepCount()
        {
            var n = 0;
            if (Contacts != null)
            {
                n += Contacts.Count;
            }

            if (Left != null)
            {
                n += Left.DeepCount();
            }

            if (Right != null)
            {
                n += Right.DeepCount();
            }

            return n;
        }

        internal IEnumerable<T> AllContacts()
        {
            if (Contacts != null)
            {
                foreach (var contact in Contacts)
                {
                    yield return contact;
                }
            }

            if (Left != null)
            {
                foreach (var contact in Left.AllContacts())
                {
                    yield return contact;
                }
            }

            if (Right == null)
            {
                yield break;
            }

            foreach (var contact in Right.AllContacts())
            {
                yield return contact;
            }
        }
    }
}
