using System;
using System.Collections.Concurrent;
using Catalyst.Common.Interfaces.P2P.Discovery;

namespace Catalyst.Node.Core.P2P.Discovery
{
    public interface IHastingCareTaker
    {
        ConcurrentQueue<IHastingMemento> HastingMementoList { get; }

        /// <summary>
        ///     Adds a new state from the walk to the queue
        /// </summary>
        /// <param name="hastingMemento"></param>
        void Add(IHastingMemento hastingMemento);

        /// <summary>
        ///     Gets the last state of the walk from the queue
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        IHastingMemento Get();
    }
}
