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
using System.Reflection;
using Xunit.Abstractions;

namespace Catalyst.TestUtils
{
    /// <summary>
    /// A test that is aware of its name, and has a reference to the test output
    /// </summary>
    public class SelfAwareTestBase
    {
        protected ITest CurrentTest;
        protected string CurrentTestName;
        protected ITestOutputHelper Output;

        protected SelfAwareTestBase(ITestOutputHelper output)
        {
            Output = output;
            CurrentTest = Output.GetType()
               .GetField("test", BindingFlags.Instance | BindingFlags.NonPublic)
               .GetValue(Output) as ITest;

            if (CurrentTest == null)
            {
                throw new ArgumentNullException(
                    $"Failed to reflect current test as {nameof(ITest)} from {nameof(output)}");
            }

            CurrentTestName = CurrentTest.TestCase.TestMethod.Method.Name;
        }
    }
}
