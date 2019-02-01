using System;
using System.Collections.Generic;
using System.Net;

namespace Catalyst.Node.Common
{
    public interface IKeyValueStore
    {
        /// <summary>
        /// </summary>
        /// <param name="host"></param>
        void Connect(IPEndPoint host);

        /// <summary>
        /// Sets the <see cref="value"/>for the given <see cref="key"/>> if it doesn't exist yet in the store
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="expiry"></param>
        /// <returns></returns>
        bool Set(byte[] key, byte[] value, TimeSpan? expiry);

        /// <summary>
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        byte[] Get(byte[] value);

        /// <summary>
        /// </summary>
        /// <returns></returns>
        Dictionary<string, string> GetInfo();
    }
}