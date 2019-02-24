using System;
using System.Runtime.Serialization;

namespace Catalyst.Node.Common.Exceptions
{
    /// <summary>
    /// </summary>
    [Serializable]
    public class UnsupportedPlatformException : Exception, ISerializable
    {
        /// <summary>
        ///     Initializes new instance of UnsupportedPlatformException class
        /// </summary>
        /// <param name="message"></param>
        public UnsupportedPlatformException(string message) : base(message) { }
    }
}