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

namespace Catalyst.Common.FileTransfer
{
    /// <summary>
    /// The file task helper
    /// </summary>
    internal static class FileTaskHelper
    {
        /// <summary>Runs the specified action.</summary>
        /// <param name="action">The action.</param>
        /// <param name="period">The period.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        internal static async Task Run(Action action, TimeSpan period, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(period, cancellationToken).ConfigureAwait(false);

                if (!cancellationToken.IsCancellationRequested)
                {
                    action();
                }
            }
        }

        /// <summary>Runs the specified action.</summary>
        /// <param name="action">The action.</param>
        /// <param name="period">The period.</param>
        /// <returns></returns>
        public static Task Run(Action action, TimeSpan period)
        {
            return Run(action, period, CancellationToken.None);
        }
    }
}
