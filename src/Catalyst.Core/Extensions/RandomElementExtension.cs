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
using System.Collections.Generic;
using System.Linq;
using Dawn;

namespace Catalyst.Core.Extensions
{
    public static class RandomElementExtension
    {
        private static readonly Random Rng = new Random();

        /// <summary>
        ///     Takes a random sample from list, must have more than 3 items in list, to take a sample of at least 2
        /// </summary>
        /// <param name="list"></param>
        /// <param name="sampleSize"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static IList<T> RandomSample<T>(this IEnumerable<T> list, int sampleSize)
        {
            var value = list.ToList();
            Guard.Argument(value).MinCount(3);
            Guard.Argument(sampleSize, nameof(sampleSize)).NotNegative().Min(2);
            
            return value.Shuffle().Take(sampleSize).ToList();
        }
        
        /// <summary>
        ///     Takes a random element
        /// </summary>
        /// <param name="list"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T RandomElement<T>(this IEnumerable<T> list)
        {
            var value = list.ToList();
            Guard.Argument(value).MinCount(1);
            var enumerable = list as T[] ?? value.ToArray();
            return enumerable[Rng.Next(enumerable.Length)];
        }

        /// <summary>
        ///     Randomises order of list
        /// </summary>
        /// <param name="source"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static IList<T> Shuffle<T>(this IEnumerable<T> source)
        {
            var enumerable = source as T[] ?? source.ToArray();
            Guard.Argument(enumerable, nameof(source)).NotNull();
            var list = source as List<T> ?? enumerable.ToList();

            var randomlyMapped = Enumerable.Range(0, list.Count)
               .Select(i => new {Index = i, SortingKey = Rng.Next()})
               .OrderBy(z => z.SortingKey)
               .Select(z => list[z.Index])
               .ToList();

            return randomlyMapped;
        }
    }
}
