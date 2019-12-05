using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;

namespace Catalyst.KBucket
{
    /// <summary>
    ///   The contacts that should be checked.
    /// </summary>
    /// <seealso cref="Ping"/>
    public class PingEventArgs<T> : EventArgs where T : IContact
    {
        /// <summary>
        ///   The contacts that should be checked.
        /// </summary>
        public IEnumerable<T> Oldest;

        /// <summary>
        ///   A new contact that wants to be added.
        /// </summary>
        public T Newest;
    }
}
