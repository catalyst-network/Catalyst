#region LICENSE

/**
* Copyright (c) 2024 Catalyst Network
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

using NUnit.Framework;
using NUnit.Framework.Interfaces;
using System;
using System.Reflection;

namespace Catalyst.TestUtils
{
    /// <summary>
    /// A test that is aware of its name, and has a reference to the test output
    /// </summary>
    public class SelfAwareTestBase
    {
        protected ITest CurrentTest;
        protected string CurrentTestName;
        protected TestContext Output;

        protected SelfAwareTestBase()
        {

        }

        public virtual void Setup(TestContext output)
        {
            Output = output;

            CurrentTestName = output.Test.MethodName;
        }
    }
}
