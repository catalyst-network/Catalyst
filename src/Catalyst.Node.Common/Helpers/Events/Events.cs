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
using System.Reflection;
using System.Threading.Tasks;
using Dawn;
using Serilog;

namespace Catalyst.Node.Common.Helpers.Events
{
    public static class Events
    {
        private static readonly ILogger Logger = Log.Logger
           .ForContext(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// </summary>
        /// <param name="handler"></param>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        /// <typeparam name="T"></typeparam>
        public static Task AsyncRaiseEvent<T>(EventHandler<T> handler, object sender, T args) where T : EventArgs
        {
            Guard.Argument(args, nameof(args)).NotNull();
            Guard.Argument(sender, nameof(sender)).NotNull();
            Guard.Argument(handler, nameof(handler)).NotNull();
            var asyncRaiseEvent = Task.Factory.StartNew(() => { handler(sender, args); });
            Logger.Debug("Raised async event of type {0}", typeof(T));
            return asyncRaiseEvent;
        }
    }
}