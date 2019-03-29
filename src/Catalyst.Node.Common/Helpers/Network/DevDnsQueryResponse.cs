/*
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

using System;
using System.Collections.Generic;
using DnsClient;
using DnsClient.Protocol;

namespace Catalyst.Node.Common.Helpers.Network {
    public class DevDnsQueryResponse : IDnsQueryResponse
    {
        public IReadOnlyList<DnsQuestion> Questions { get; }
        public IReadOnlyList<DnsResourceRecord> Additionals { get; }
        public IEnumerable<DnsResourceRecord> AllRecords { get; }
        public IReadOnlyList<DnsResourceRecord> Answers { get; set;  }
        public IReadOnlyList<DnsResourceRecord> Authorities { get; }
        public string AuditTrail { get; }
        public string ErrorMessage { get; }
        public bool HasError { get; }
        public DnsResponseHeader Header { get; }
        public int MessageSize { get; }
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
                    new[] { value }, new[] { value }
                )
            };
        }
    }
}
