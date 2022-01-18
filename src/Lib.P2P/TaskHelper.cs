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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Lib.P2P
{
    /// <summary>
    ///   Some helpers for tasks.
    /// </summary>
    public static class TaskHelper
    {
        /// <summary>
        ///   Gets the first result from a set of tasks.
        /// </summary>
        /// <typeparam name="T">
        ///   The result type of the <paramref name="tasks"/>.
        /// </typeparam>
        /// <param name="tasks">
        ///   The tasks to perform.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation. The task's result is
        ///   a <typeparamref name="T"/>>.
        /// </returns>
        /// <remarks>
        ///   Returns the result of the first task that is not
        ///   faulted or canceled.
        /// </remarks>
        public static async Task<T> WhenAnyResultAsync<T>(IEnumerable<Task<T>> tasks,
            CancellationToken cancel)
        {
            List<Exception> exceptions = new();
            var running = tasks.ToList();
            while (running.Count > 0)
            {
                cancel.ThrowIfCancellationRequested();
                var winner = await Task.WhenAny(running).ConfigureAwait(false);
                if (!winner.IsCanceled && !winner.IsFaulted)
                {
                    return await winner;
                }
                
                if (winner.IsFaulted)
                {
                    if (winner.Exception is { } ae)
                    {
                        exceptions.AddRange(ae.InnerExceptions);
                    }
                    else
                    {
                        exceptions.Add(winner.Exception);
                    }
                }

                running.Remove(winner);
            }

            cancel.ThrowIfCancellationRequested();
            throw new AggregateException("No task(s) returned a result.", exceptions);
        }

        /// <summary>
        ///   Run async tasks in parallel,
        /// </summary>
        /// <param name="source">
        ///   A sequence of some data.
        /// </param>
        /// <param name="funcBody">
        ///   The async code to perform.
        /// </param>
        /// <param name="maxDoP">
        ///   The number of partitions to create.
        /// </param>
        /// <returns>
        ///   A Task to await.
        /// </returns>
        /// <remarks>
        ///   Copied from https://houseofcat.io/tutorials/csharp/async/parallelforeachasync
        /// </remarks>
        internal static Task ParallelForEachAsync<T>(this IEnumerable<T> source, Func<T, Task> funcBody, int maxDoP = 4)
        {
            async Task AwaitPartition(IEnumerator<T> partition)
            {
                using (partition)
                {
                    while (partition.MoveNext()) await funcBody(partition.Current);
                }
            }

            return Task.WhenAll(
                Partitioner
                   .Create(source)
                   .GetPartitions(maxDoP)
                   .AsParallel()
                   .Select(AwaitPartition));
        }
    }
}
