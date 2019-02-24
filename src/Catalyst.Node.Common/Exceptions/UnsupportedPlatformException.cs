using System;

namespace Catalyst.Node.Common.Exceptions
{
    /// <summary>
    /// </summary>
    public class UnsupportedPlatformException : Exception
    {
        /// <summary>
        ///     Initializes new instance of UnsupportedPlatformException class
        /// </summary>
        /// <param name="message"></param>
        public UnsupportedPlatformException(string message) : base(message) { }
    }
}