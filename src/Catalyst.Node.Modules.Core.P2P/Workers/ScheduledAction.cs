using System;
using System.Collections.Generic;
using Catalyst.Helpers.Logger;

namespace Catalyst.Node.Modules.Core.P2P.Workers
{
    internal class ScheduledAction : IComparable<ScheduledAction>
    {
        private static readonly Queue<ScheduledAction> Pool = new Queue<ScheduledAction>();
        public Action Action { get; private set; }
        public TimeSpan Interval { get; private set; }
        public DateTime NextExecutionDate { get; set; }
        public bool Repeat { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        private ScheduledAction(){}

        /// <summary>
        /// 
        /// </summary>
        public void Execute()
        {
            Log.Message("excecuting scheduled action");
            Action();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public int CompareTo(ScheduledAction other)
        {
            if (other == this) return 0;

            var diff = NextExecutionDate.CompareTo(other.NextExecutionDate);
            return (diff >= 0) ? 1 : -1;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="action"></param>
        /// <param name="interval"></param>
        /// <param name="repeat"></param>
        /// <returns></returns>
        public static ScheduledAction Create(Action action, TimeSpan interval, bool repeat)
        {
            Log.Message("creating scheduled action");
            Log.Message(action.ToString());
            Log.Message(repeat.ToString());
            Log.Message(interval.ToString());

            var sa = Pool.Count > 0 ? Pool.Dequeue() : new ScheduledAction();
            sa.Action = action;
            sa.Interval = interval;
            sa.NextExecutionDate = DateTime.UtcNow + interval;
            sa.Repeat = repeat;
            return sa;
        }

        /// <summary>
        /// 
        /// </summary>
        public void Release()
        {
            Pool.Enqueue(this);
        }
    }
}