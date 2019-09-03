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
using System.Diagnostics;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Catalyst.TestUtils
{
    public static class TaskHelper
    {
        public static async Task<bool> WaitForAsync(Func<bool> condition, TimeSpan timespan)
        {
            CancellationTokenSource tokenSource;
            using (tokenSource = new CancellationTokenSource(timespan))
            {
                var success = await Task.Run(() =>
                {
                    while (!tokenSource.IsCancellationRequested)
                    {
                        if (!condition())
                        {
                            Task.Delay(100).ConfigureAwait(false).GetAwaiter().GetResult();
                        }
                        else
                        {
                            return true;
                        }
                    }

                    return false;
                }, tokenSource.Token).ConfigureAwait(false);

                return success;
            }
        }

        public static async Task WaitForAsyncOrThrow(Expression<Func<bool>> condition,
            TimeSpan timeout = default,
            TimeSpan waitPeriod = default)
        {
            var timeoutDefaulted = timeout == default ? TimeSpan.FromSeconds(2) : timeout;
            var delay = waitPeriod == default ? TimeSpan.FromMilliseconds(100) : waitPeriod;

            var performCheck = condition.Compile();
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            while (!performCheck())
            {
                stopWatch.Elapsed.Should().BeLessOrEqualTo(timeoutDefaulted,
                    $"{condition} should have become {true} after {timeoutDefaulted} ms.");

                await Task.Delay(delay).ConfigureAwait(false);
            }
        }
    }

    public class TaskHelperTests
    {
        private readonly ITestOutputHelper _output;
        public TaskHelperTests(ITestOutputHelper output) { _output = output; }

        [Fact]
        public async Task WaitFor_Should_Throw_Descriptive_Message_After_Timeout()
        {
            var attempts = 0;

            var waitDelay = TimeSpan.FromMilliseconds(100);
            var timeout = TimeSpan.FromMilliseconds(500);

            var watch = new Stopwatch();
            watch.Start();
            var success = await TaskHelper.WaitForAsync(
                    () => IncreaseAndCheckIfAboveLimit(ref attempts,
                        (int) (timeout.TotalMilliseconds / waitDelay.TotalMilliseconds) + 1), timeout)
               .ConfigureAwait(false);
            watch.Stop();

            success.Should().BeFalse();
            attempts.Should().BeGreaterOrEqualTo(5);
            watch.Elapsed.Should().BeGreaterOrEqualTo(timeout);
        }

        private bool IncreaseAndCheckIfAboveLimit(ref int count, int limit)
        {
            _output.WriteLine($"attempt: {++count}");
            return limit < count;
        }

        [Fact]
        public async Task WaitFor_Should_Return_As_Soon_As_Possible()
        {
            var attempts = 0;

            var timeout = TimeSpan.FromSeconds(1);

            var watch = new Stopwatch();
            watch.Start();
            var success = await TaskHelper.WaitForAsync(
                    () => IncreaseAndCheckIfAboveLimit(ref attempts, 2), timeout)
               .ConfigureAwait(false);
            watch.Stop();

            success.Should().BeTrue();
            attempts.Should().BeLessOrEqualTo(3);
            watch.Elapsed.Should().BeLessOrEqualTo(timeout.Multiply(2));
        }

        [Fact]
#pragma warning disable 1998
        public async Task WaitForAsyncOrThrow_Should_Throw_Descriptive_Message_After_Timeout()
#pragma warning restore 1998
        {
            var attempts = 0;

            var waitDelay = TimeSpan.FromMilliseconds(50);
            var timeout = TimeSpan.FromMilliseconds(500);

            var watch = new Stopwatch();
            watch.Start();
            new Func<Task>(async () => await TaskHelper.WaitForAsyncOrThrow(
                        () => IncreaseAndCheckIfAboveLimit(ref attempts,
                            (int) (timeout.TotalMilliseconds / waitDelay.TotalMilliseconds) + 1), timeout, waitDelay)
                   .ConfigureAwait(false)).Should().Throw<XunitException>()
               .And.Message.Should().Contain(nameof(IncreaseAndCheckIfAboveLimit));
            watch.Stop();

            attempts.Should().BeGreaterOrEqualTo(9);
            watch.Elapsed.Should().BeGreaterOrEqualTo(timeout);
        }

        [Fact]
        public async Task WaitForAsyncOrThrow_Should_Return_As_Soon_As_Possible()
        {
            var attempts = 0;

            var timeout = TimeSpan.FromSeconds(1);
            var waitDelay = TimeSpan.FromMilliseconds(50);

            var watch = new Stopwatch();
            watch.Start();
            await TaskHelper.WaitForAsyncOrThrow(
                    () => IncreaseAndCheckIfAboveLimit(ref attempts, 2), timeout, waitDelay)
               .ConfigureAwait(false);
            watch.Stop();

            attempts.Should().BeLessOrEqualTo(3);
            watch.Elapsed.Should().BeLessOrEqualTo(timeout.Multiply(2));
        }
    }
}
