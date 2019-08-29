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

using System.Collections.Generic;
using DnsClient;
using DnsClient.Protocol;

namespace Catalyst.Core.Network
{
    public sealed class DevDnsQueryResponse : IDnsQueryResponse
    {
        // ReSharper disable once UnassignedGetOnlyAutoProperty
        public IReadOnlyList<DnsQuestion> Questions { get; }

        // ReSharper disable once UnassignedGetOnlyAutoProperty
        public IReadOnlyList<DnsResourceRecord> Additionals { get; }

        // ReSharper disable once UnassignedGetOnlyAutoProperty
        public IEnumerable<DnsResourceRecord> AllRecords { get; }

        // ReSharper disable once MemberCanBeInternal
        public IReadOnlyList<DnsResourceRecord> Answers { get; set; }

        // ReSharper disable once UnassignedGetOnlyAutoProperty
        public IReadOnlyList<DnsResourceRecord> Authorities { get; }

        // ReSharper disable once UnassignedGetOnlyAutoProperty
        public string AuditTrail { get; }

        // ReSharper disable once UnassignedGetOnlyAutoProperty
        public string ErrorMessage { get; }

        // ReSharper disable once UnassignedGetOnlyAutoProperty
        public bool HasError { get; }

        // ReSharper disable once UnassignedGetOnlyAutoProperty
        public DnsResponseHeader Header { get; }

        // ReSharper disable once UnassignedGetOnlyAutoProperty
        public int MessageSize { get; }

        // ReSharper disable once UnassignedGetOnlyAutoProperty
        public NameServer NameServer { get; }

        /// <summary>
        ///     Builds a mocked TXT resource record from a ILookup DNS query.
        /// </summary>
        /// <param name="domainName"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static List<DnsResourceRecord> BuildDnsResourceRecords(string domainName, string value)
        {
            return new List<DnsResourceRecord>
            {
                new TxtRecord(
                    new ResourceRecordInfo(domainName, ResourceRecordType.TXT, QueryClass.CS, 10, 32),
                    new[]
                    {
                        value
                    }, new[]
                    {
                        value
                    }
                )
            };
        }
    }
}
