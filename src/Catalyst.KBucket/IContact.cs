#region LICENSE

// Copyright (c) 2024 Catalyst Network
//
// This file is part of Catalyst.Node href="https://github.com/catalyst-network/Catalyst.Node"
//
// Catalyst.Node is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 2 of the License, or
// (at your option) any later version.

// Catalyst.Node is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.

// You should have received a copy of the GNU General Public License
// along with Catalyst.Node. If not, see href="https://www.gnu.org/licenses/.

#endregion

namespace Catalyst.KBucket
{
    /// <summary>
    ///   A peer/node in the distributed system.
    /// </summary>
    public interface IContact
    {
        /// <summary>
        ///   Unique identifier of the contact.
        /// </summary>
        /// <value>
        ///   Typically a hash of a unique identifier. 
        /// </value>
        byte[] Id { get; }
    }
}
