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

using Catalyst.Abstractions.Enumerator;

namespace Catalyst.Abstractions.Types
{
    public class NeighbourStateTypes
        : Enumeration
    {
        public static readonly NeighbourStateTypes Contacted = new ContactedStateTypes();
        public static readonly NeighbourStateTypes Responsive = new ResponsiveStateTypes();
        public static readonly NeighbourStateTypes NotContacted = new NotContactedStateTypes();
        public static readonly NeighbourStateTypes UnResponsive = new UnResponsiveStateTypes();

        private NeighbourStateTypes(int id, string name) : base(id, name) { }

        private sealed class NotContactedStateTypes : NeighbourStateTypes
        {
            public NotContactedStateTypes() : base(1, "NotContacted") { }
        }
        
        private sealed class ContactedStateTypes : NeighbourStateTypes
        {
            public ContactedStateTypes() : base(2, "Contacted") { }
        }
        
        private sealed class UnResponsiveStateTypes : NeighbourStateTypes
        {
            public UnResponsiveStateTypes() : base(3, "UnResponsive") { }
        }
        
        private sealed class ResponsiveStateTypes : NeighbourStateTypes
        {
            public ResponsiveStateTypes() : base(4, "Responsive") { }
        }
    }
}
