using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using Catalyst.Helpers.Logger;
using Catalyst.Helpers.Util;
using DnsClient;
using DnsClient.Protocol;

namespace Catalyst.Helpers.Network
{
    public class Dns
    {   
        /// <summary>
        /// 
        /// </summary>
        /// <param name="hostnames"></param>
        /// <returns></returns>
        public static IList<IDnsQueryResponse> GetTxtRecords(List<string> hostnames)
        {
            //@TODO guard util
            var recordList = new List<IDnsQueryResponse>();
            foreach (var hostname in hostnames)
            {
                recordList.Add(GetTxtRecords(hostname));
            }
            
            return recordList;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hostname"></param>
        /// <returns></returns>
        public static IDnsQueryResponse GetTxtRecords(Uri hostname)
        {
            //@TODO guard util
            return GetTxtRecords(hostname.Host);
        }
        
        /// <summary>
        /// </summary>
        /// <param name="hostname"></param>
        /// <returns></returns>
        public static IDnsQueryResponse GetTxtRecords(string hostname)
        {
            Log.Message(hostname);
            Guard.NotNull(hostname, nameof(hostname));
            Guard.NotEmpty(hostname, nameof(hostname));
            
            var endpoint = new IPEndPoint(IPAddress.Parse("9.9.9.9"), 53); //@TODO get these from settings
            var client = new LookupClient(endpoint);
            var result = client.Query(hostname, QueryType.TXT);
            return result;
        }
    }
}