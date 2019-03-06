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
using Catalyst.Node.Common.Interfaces;

namespace Catalyst.Node.Common.Helpers.Workers
{
    public class ClientWorker : IWorker, IWorkScheduler
    {
        private readonly BackgroundWorker _backgroundWorker;
        private readonly TimedWorker _timedWorker;

        /// <summary>
        /// </summary>
        public ClientWorker()
        {
            _timedWorker = new TimedWorker();
            _backgroundWorker = new BackgroundWorker();
        }

        /// <summary>
        /// </summary>
        /// <param name="action"></param>
        public void Queue(Action action) { _backgroundWorker.Queue(action); }

        /// <summary>
        /// </summary>
        public void Start()
        {
            _backgroundWorker.Start();
            _timedWorker.Start();
        }

        /// <summary>
        /// </summary>
        public void Stop()
        {
            _timedWorker.Stop();
            _backgroundWorker.Stop();
        }

        /// <summary>
        /// </summary>
        /// <param name="action"></param>
        /// <param name="interval"></param>
        public void QueueForever(Action action, TimeSpan interval)
        {
            _timedWorker.QueueForever(() => _backgroundWorker.Queue(action), interval);
        }

        /// <summary>
        /// </summary>
        /// <param name="action"></param>
        /// <param name="interval"></param>
        public void QueueOneTime(Action action, TimeSpan interval)
        {
            _timedWorker.QueueOneTime(() => _backgroundWorker.Queue(action), interval);
        }
    }
}