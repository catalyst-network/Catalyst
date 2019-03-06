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
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Node.Common.Interfaces;

namespace Catalyst.Node.Common.Helpers.Workers
{
    internal class BackgroundWorker : IWorker, IDisposable
    {
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly BlockingQueue<Action> _queue;

        public BackgroundWorker()
        {
            _queue = new BlockingQueue<Action>();
            _cancellationTokenSource = new CancellationTokenSource();
            Start();
        }

        public void Dispose() { Dispose(true); }

        public void Start()
        {
            Task.Factory.StartNew(() =>
                {
                    while (!_cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        var action = _queue.Take();
                        action();
                    }
                }, _cancellationTokenSource.Token, TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
        }

        public void Stop() { _cancellationTokenSource.Cancel(); }

        public void Queue(Action action) { _queue.Add(action); }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _cancellationTokenSource?.Dispose();
                _queue?.Dispose();
            }
        }
    }
}