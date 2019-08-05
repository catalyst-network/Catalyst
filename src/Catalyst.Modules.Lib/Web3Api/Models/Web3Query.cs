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

using System.Text;

namespace Catalyst.Modules.Lib.Web3Api.Models
{
    public sealed class Web3Query
    {
        public string OperationName { get; set; }
        public string NamedQuery { get; set; }
        public string Query { get; set; }
        public string Variables { get; set; }

        public override string ToString()
        {
            var builder = new StringBuilder();
            
            builder.AppendLine();
            
            if (!string.IsNullOrWhiteSpace(OperationName))
            {
                builder.AppendLine($"OperationName = {OperationName}");
            }
            
            if (!string.IsNullOrWhiteSpace(NamedQuery))
            {
                builder.AppendLine($"NamedQuery = {NamedQuery}");
            }
            
            if (!string.IsNullOrWhiteSpace(Query))
            {
                builder.AppendLine($"Query = {Query}");
            }
            
            if (!string.IsNullOrWhiteSpace(Variables))
            {
                builder.AppendLine($"Variables = {Variables}");
            }

            return builder.ToString();
        }
    }
}
