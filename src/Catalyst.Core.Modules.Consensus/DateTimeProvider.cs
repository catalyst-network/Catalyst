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
using Catalyst.Abstractions.Consensus;

namespace Catalyst.Core.Modules.Consensus
{
    /// <inheritdoc />
    public sealed class DateTimeProvider : IDateTimeProvider
    {
        /// <inheritdoc />
        public DateTime UtcNow => DateTime.UtcNow;

        public TimeSpan GetTimeSpanUntilNextCycleStart(TimeSpan cycleDuration)
        {
            var cycleDurationTicks = DateTime.UtcNow.Ticks % cycleDuration.Ticks;
            var ticksUntilNextCycleStart = cycleDurationTicks == 0
                ? 0
                : cycleDuration.Ticks - cycleDurationTicks;
            return TimeSpan.FromTicks(ticksUntilNextCycleStart);
        }

        public DateTime GetDateUntilNextCycleStart(TimeSpan cycleDuration)
        {
            return UtcNow.Add(GetTimeSpanUntilNextCycleStart(cycleDuration));
        }
    }
}
