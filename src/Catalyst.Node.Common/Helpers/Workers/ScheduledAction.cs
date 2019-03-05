using System;
using System.Collections.Generic;
using Serilog;

namespace Catalyst.Node.Common.Helpers.Workers
{
    internal class ScheduledAction : IComparable<ScheduledAction>
    {
        private static readonly ILogger Logger = Log.Logger.ForContext(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

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
        public void Release()
        {
            Pool.Enqueue(this);
        }
    }
}