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
using System.Linq;
using System.Reflection;
using Serilog;

namespace Catalyst.Core.Modules.Web3.Controllers.Handlers
{
    public class Web3HandlerResolver : IWeb3HandlerResolver
    {
        private readonly ILogger _logger = Log.Logger.ForContext(typeof(Web3HandlerResolver));

        private Dictionary<string, EthWeb3RequestHandlerBase> _handlers = new(StringComparer.InvariantCultureIgnoreCase);

        public Web3HandlerResolver()
        {
            var handlers = typeof(EthController).Assembly.GetTypes()
               .Select(t => new {HandlerType = t, HandlerDetails = t.GetCustomAttribute<EthWeb3RequestHandlerAttribute>()})
               .Where(h => h.HandlerDetails != null);

            foreach (var handler in handlers)
            {
                if (!typeof(EthWeb3RequestHandlerBase).IsAssignableFrom(handler.HandlerType))
                {
                    _logger.Error($"Handler {{handlerType}} is not {nameof(IWeb3HandlerResolver)}", handler.HandlerType);
                }

                _handlers[handler.HandlerDetails.FullMethod] = (EthWeb3RequestHandlerBase) Activator.CreateInstance(handler.HandlerType);
            }
        }

        public EthWeb3RequestHandlerBase Resolve(string fullMethod, int paramsCount)
        {
            return _handlers[fullMethod];
        }
    }
}
