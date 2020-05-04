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
using System.Linq;
using NUnit.Framework;

namespace Catalyst.Core.Modules.Dfs.Tests.IntegrationTests
{
    /// <summary>
    ///   Asserting an <see cref="Exception"/>.
    /// </summary>
    internal static class ExceptionAssert
    {
        internal static T Throws<T>(Action action, string expectedMessage = null) where T : Exception
        {
            try
            {
                action();
            }
            catch (AggregateException e)
            {
                var match = e.InnerExceptions.OfType<T>().FirstOrDefault();
                if (match == null)
                {
                    throw;
                }
                
                if (expectedMessage != null)
                {
                    Assert.AreEqual(expectedMessage, match.Message);
                }
                    
                return match;
            }
            catch (T e)
            {
                if (expectedMessage != null)
                {
                    Assert.AreEqual(expectedMessage, e.Message);
                }
                
                return e;
            }

            throw new Exception($"Exception of type {typeof(T)}should be thrown.");
        }

        public static Exception Throws(Action action, string expectedMessage = null)
        {
            return Throws<Exception>(action, expectedMessage);
        }
    }
}
