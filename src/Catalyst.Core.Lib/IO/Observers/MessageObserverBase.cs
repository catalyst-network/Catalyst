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
using System.Reactive.Linq;
using Catalyst.Abstractions.IO.Observers;
using Serilog;

namespace Catalyst.Core.Lib.IO.Observers
{
    public abstract class MessageObserverBase<T> : IMessageObserver<T>, IDisposable
    {
        protected readonly ILogger Logger;
        protected IDisposable MessageSubscription;
        private readonly Func<T, bool> _filterExpression;

        protected MessageObserverBase(ILogger logger, Func<T, bool> filterExpression)
        {
            Logger = logger;
            _filterExpression = filterExpression;
        }

        public void StartObserving(IObservable<T> messageStream)
        {
            if (MessageSubscription != null)
            {
                return;
            }

            MessageSubscription = messageStream.Where(_filterExpression).Subscribe(this);
        }

        public abstract void OnNext(T message);

        public virtual void OnCompleted()
        {
            Logger.Debug("Message stream ended.");
        }

        public virtual void OnError(Exception exception)
        {
            Logger.Error(exception, "Failed to process message.");
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            MessageSubscription?.Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}
