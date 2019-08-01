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

using Catalyst.Common.Enumerator;

namespace Catalyst.Common.Config
{
    public class NeighbourState
        : Enumeration
    {
        public static readonly NeighbourState Contacted = new ContactedState();
        public static readonly NeighbourState Responsive = new ResponsiveState();
        public static readonly NeighbourState NotContacted = new NotContactedState();
        public static readonly NeighbourState UnResponsive = new UnResponsiveState();

        private NeighbourState(int id, string name) : base(id, name) { }

        private sealed class NotContactedState : NeighbourState
        {
            public NotContactedState() : base(1, "NotContacted") { }
        }
        
        private sealed class ContactedState : NeighbourState
        {
            public ContactedState() : base(2, "Contacted") { }
        }
        
        private sealed class UnResponsiveState : NeighbourState
        {
            public UnResponsiveState() : base(3, "UnResponsive") { }
        }
        
        private sealed class ResponsiveState : NeighbourState
        {
            public ResponsiveState() : base(4, "Responsive") { }
        }
    }
}
