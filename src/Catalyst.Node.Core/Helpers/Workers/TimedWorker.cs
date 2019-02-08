using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Catalyst.Node.Core.Helpers.Workers
{
    internal class TimedWorker : IWorkScheduler
    {
        private readonly List<ScheduledAction> _actions = new List<ScheduledAction>();
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly AutoResetEvent _resetEvent = new AutoResetEvent(false);

        /// <summary>
        /// </summary>
        public TimedWorker()
        {
            _cancellationTokenSource = new CancellationTokenSource();
        }

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
                                              if (any) scheduledAction = _actions[0];
                                          }

                                          TimeSpan timeToWait;
                                          if (any)
                                          {
                                              var runTime = scheduledAction.NextExecutionDate;
                                              var dT = runTime - DateTime.UtcNow;
                                              timeToWait = dT > TimeSpan.Zero ? dT : TimeSpan.Zero;
                                          }
                                          else
                                          {
                                              timeToWait = TimeSpan.FromMilliseconds(-1);
                                          }

                                          if (_resetEvent.WaitOne(timeToWait, false)) continue;

                                          Debug.Assert(scheduledAction != null, "scheduledAction != null");
                                          scheduledAction.Execute();
                                          lock (_actions)
                                          {
                                              Remove(scheduledAction);
                                              if (scheduledAction.Repeat)
                                                  QueueForever(scheduledAction.Action, scheduledAction.Interval);
                                          }
                                      }
                                  }, _cancellationTokenSource.Token, TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
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
                if (pos == 0) _resetEvent.Set();
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

                if (pos == 0) _resetEvent.Set();
            }
        }

        /// <summary>
        /// </summary>
        public void Stop()
        {
            _cancellationTokenSource.Cancel();
        }
    }
}