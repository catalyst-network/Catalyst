using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Catalyst.Helpers.Util
{
    public static class Guard
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="o"></param>
        /// <param name="paramName"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public static void NotNull(object o, string paramName)
        {
            if (o == null) throw new ArgumentNullException(paramName);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="str"></param>
        /// <param name="paramName"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public static void NotEmpty(string str, string paramName)
        {
            if (string.IsNullOrEmpty(str)) throw new ArgumentNullException(paramName);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="val"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <param name="paramName"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static void IsBetween(int val, int min, int max, string paramName)
        {
            if (val < min || val > max) throw new ArgumentOutOfRangeException(paramName);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="val"></param>
        /// <param name="min"></param>
        /// <param name="paramName"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static void IsGreaterOrEqualTo(int val, int min, string paramName)
        {
            if (val < min) throw new ArgumentOutOfRangeException(paramName);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dict"></param>
        /// <param name="key"></param>
        /// <param name="message"></param>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TQ"></typeparam>
        /// <exception cref="ArgumentException"></exception>
        public static void ContainsKey<T,TQ>(IDictionary<T, TQ> dict, T key, string message)
        {
            if (!dict.ContainsKey(key)) throw new ArgumentException(message);
        }
    }
}
