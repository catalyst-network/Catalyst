using System;
using System.Linq;
using Xunit;
using Xunit.Sdk;

namespace Catalyst.Core.Modules.Dfs.Tests
{
    /// <summary>
    ///   Asserting an <see cref="Exception"/>.
    /// </summary>
    public static class ExceptionAssert
    {
        public static T Throws<T>(Action action, string expectedMessage = null) where T : Exception
        {
            try
            {
                action();
            }
            catch (AggregateException e)
            {
                var match = e.InnerExceptions.OfType<T>().FirstOrDefault();
                if (match != null)
                {
                    if (expectedMessage != null)
                    {
                        Assert.Equal(expectedMessage, match.Message);
                    }
                    
                    return match;
                }

                throw;
            }
            catch (T e)
            {
                if (expectedMessage != null)
                    Assert.Equal(expectedMessage, e.Message);
                return e;
            }

            throw new XunitException($"Exception of type {typeof(T)}should be thrown.");

            //  The compiler doesn't know that Assert.Fail will always throw an exception
            return null;
        }

        public static Exception Throws(Action action, string expectedMessage = null)
        {
            return Throws<Exception>(action, expectedMessage);
        }
    }
}
