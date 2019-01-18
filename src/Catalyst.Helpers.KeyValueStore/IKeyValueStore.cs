using System;
using System.Collections.Generic;
using System.Net;

namespace Catalyst.Helpers.KeyValueStore
{
    public interface IKeyValueStore
    {
        /// <summary>
        /// </summary>
        /// <param name="host"></param>
        void Connect(IPEndPoint host);

        /// <summary>
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