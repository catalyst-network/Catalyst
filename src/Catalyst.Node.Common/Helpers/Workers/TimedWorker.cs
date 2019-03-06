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
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Node.Common.Interfaces;

namespace Catalyst.Node.Common.Helpers.Workers
{
    internal class TimedWorker : IWorkScheduler, IDisposable
    {
        private readonly List<ScheduledAction> _actions = new List<ScheduledAction>();
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly AutoResetEvent _resetEvent = new AutoResetEvent(false);

        /// <summary>
        /// </summary>
        public TimedWorker() { _cancellationTokenSource = new CancellationTokenSource(); }

        public void Dispose() { Dispose(true); }

        /// <summary>
        /// </summary>
        public void Start()
        {
            Task.Factory.StartNew(() =>
            {
                ScheduledAction scheduledAction = null;

                while (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    bool any;
                    lock (_actions)
                    {
                        any = _actions.Count > 0;
                        if (any)
                        {
                            scheduledAction = _actions[0];
                        }
                    }

                    var timeToWait = TimeSpan.Zero;
                    if (any)
                    {
                        if (scheduledAction != null)
                        {
                            var runTime = scheduledAction.NextExecutionDate;
                            var dT = runTime - DateTime.UtcNow;
                            timeToWait = dT > TimeSpan.Zero ? dT : TimeSpan.Zero;
                        }
                    }
                    else
                    {
                        timeToWait = TimeSpan.FromMilliseconds(-1);
                    }

                    if (_resetEvent.WaitOne(timeToWait, false))
                    {
                        continue;
                    }

                    Debug.Assert(scheduledAction != null, "scheduledAction != null");
                    scheduledAction.Execute();
                    lock (_actions)
                    {
                        Remove(scheduledAction);
                        if (scheduledAction.Repeat)
                        {
                            QueueForever(scheduledAction.Action, scheduledAction.Interval);
                        }
                    }
                }
            }, _cancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        /// <summary>
        /// </summary>
        /// <param name="action"></param>
        /// <param name="interval"></param>
        public void QueueForever(Action action, TimeSpan interval)
        {
            QueueInternal(ScheduledAction.Create(action, interval, true));
        }

        /// <summary>
        /// </summary>
        /// <param name="action"></param>
        /// <param name="interval"></param>
        public void QueueOneTime(Action action, TimeSpan interval)
        {
            QueueInternal(ScheduledAction.Create(action, interval, false));
        }

        /// <summary>
        /// </summary>
        /// <param name="scheduledAction"></param>
        private void Remove(ScheduledAction scheduledAction)
        {
            lock (_actions)
            {
                var pos = _actions.BinarySearch(scheduledAction);
                _actions.RemoveAt(pos);
                scheduledAction.Release();
                if (pos == 0)
                {
                    _resetEvent.Set();
                }
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="scheduledAction"></param>
        private void QueueInternal(ScheduledAction scheduledAction)
        {
            lock (_actions)
            {
                var pos = _actions.BinarySearch(scheduledAction);
                pos = pos >= 0 ? pos : ~pos;
                _actions.Insert(pos, scheduledAction);

                if (pos == 0)
                {
                    _resetEvent.Set();
                }
            }
        }

        /// <summary>
        /// </summary>
        public void Stop() { _cancellationTokenSource.Cancel(); }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _cancellationTokenSource?.Dispose();
                _resetEvent?.Dispose();
            }
        }
    }
}