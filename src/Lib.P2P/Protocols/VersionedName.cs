#region LICENSE

/**
* Copyright (c) 2022 Catalyst Network
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
using Semver;

namespace Lib.P2P.Protocols
{
    /// <summary>
    ///   A name with a semantic version.
    /// </summary>
    /// <remarks>
    ///   Implements value type equality.
    /// </remarks>
    public sealed class VersionedName : IEquatable<VersionedName>, IComparable<VersionedName>
    {
        /// <summary>
        ///   The name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///   The semantic version.
        /// </summary>
        public SemVersion Version { get; set; }

        /// <inheritdoc />
        public override string ToString() { return $"/{Name}/{Version}"; }

        /// <summary>
        ///   Parse
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static VersionedName Parse(string s)
        {
            var parts = s.Split('/').Where(p => p.Length > 0).ToArray();
            return new VersionedName
            {
                Name = string.Join("/", parts, 0, parts.Length - 1),
                Version = SemVersion.Parse(parts[^1])
            };
        }

        /// <inheritdoc />
        public override int GetHashCode() { return ToString().GetHashCode(); }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            var that = obj as VersionedName;
            return that != null && (Name == that.Name && Version == that.Version);
        }

        /// <inheritdoc />
        public bool Equals(VersionedName that) { return Name == that?.Name && Version == that?.Version; }

        /// <summary>
        ///   Value equality.
        /// </summary>
        public static bool operator ==(VersionedName a, VersionedName b)
        {
            return ReferenceEquals(a, b) || !ReferenceEquals(a, null) && !ReferenceEquals(b, null) && a.Equals(b);
        }

        /// <summary>
        ///   Value inequality.
        /// </summary>
        public static bool operator !=(VersionedName a, VersionedName b)
        {
            return !ReferenceEquals(a, b) && (ReferenceEquals(a, null) || ReferenceEquals(b, null) || !a.Equals(b));
        }

        /// <inheritdoc />
        public int CompareTo(VersionedName that)
        {
            return that == null ? 1 : Name == that.Name ? Version.CompareTo(that.Version) : string.Compare(Name, that.Name, StringComparison.Ordinal);
        }
    }
}
