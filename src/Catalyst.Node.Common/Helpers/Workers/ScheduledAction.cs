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
using System.Reflection;
using Serilog;

namespace Catalyst.Node.Common.Helpers.Workers
{
    internal class ScheduledAction : IComparable<ScheduledAction>
    {
        private static readonly ILogger Logger = Log.Logger.ForContext(MethodBase.GetCurrentMethod().DeclaringType);

        private static readonly Queue<ScheduledAction> Pool = new Queue<ScheduledAction>();

        /// <summary>
        /// </summary>
        private ScheduledAction() { }

        public Action Action { get; private set; }
        public TimeSpan Interval { get; private set; }
        public DateTime NextExecutionDate { get; set; }
        public bool Repeat { get; private set; }

        /// <summary>
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public int CompareTo(ScheduledAction other)
        {
            if (other == this)
            {
                return 0;
            }

            var diff = NextExecutionDate.CompareTo(other.NextExecutionDate);
            return diff >= 0 ? 1 : -1;
        }

        /// <summary>
        /// </summary>
        public void Execute()
        {
            Logger.Debug("excecuting scheduled action");
            Action();
        }

        /// <summary>
        /// </summary>
        /// <param name="action"></param>
        /// <param name="interval"></param>
        /// <param name="repeat"></param>
        /// <returns></returns>
        public static ScheduledAction Create(Action action, TimeSpan interval, bool repeat)
        {
            var sa = Pool.Count > 0 ? Pool.Dequeue() : new ScheduledAction();
            sa.Action = action;
            sa.Interval = interval;
            sa.NextExecutionDate = DateTime.UtcNow + interval;
            sa.Repeat = repeat;
            return sa;
        }

        /// <summary>
        /// </summary>
        public void Release() { Pool.Enqueue(this); }
    }
}