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

using System;

namespace Catalyst.Core.Modules.Web3.Controllers.Handlers
{
    internal sealed class EthWeb3RequestHandlerAttribute : Attribute
    {
        private readonly string _module;
        private readonly string _method;
        public string FullMethod => $"{_module}_{_method}";

        public EthWeb3RequestHandlerAttribute(string module, string method)
        {
            _module = module;
            _method = method;
        }
    }
}