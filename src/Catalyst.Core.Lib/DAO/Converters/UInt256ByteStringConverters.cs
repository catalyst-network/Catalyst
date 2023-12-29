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

using AutoMapper;
using Catalyst.Core.Lib.Extensions;
using Google.Protobuf;
using Nethermind.Dirichlet.Numerics;

namespace Catalyst.Core.Lib.DAO.Converters
{
    public class ByteStringToUInt256StringConverter : IValueConverter<ByteString, string>
    {
        public string Convert(ByteString sourceMember, ResolutionContext context)
        {
            return sourceMember.ToUInt256().ToString();
        }
    }

    public class UInt256StringToByteStringConverter : IValueConverter<string, ByteString>
    {
        public ByteString Convert(string sourceMember, ResolutionContext context)
        {
            var sourceValue = UInt256.Parse(sourceMember).ToUint256ByteString();
            return sourceValue; 
        }
    }
}
