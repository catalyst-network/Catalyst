using System;
using System.Collections.Generic;
using System.Net;
using Dawn;
using DnsClient;

namespace Catalyst.Node.Core.Helpers.Network
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
            Guard.Argument(hostnames, nameof(hostnames)).NotEmpty().NotNull().DoesNotContainNull();
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
            Guard.Argument(hostname, nameof(hostname)).NotNull().Http();
            return GetTxtRecords(hostname.Host);
        }
        
        /// <summary>
        /// </summary>
        /// <param name="hostname"></param>
        /// <returns></returns>
        public static IDnsQueryResponse GetTxtRecords(string hostname)
        {
            Guard.Argument(hostname, nameof(hostname)).NotNull().NotEmpty().NotWhiteSpace();
            return Query(hostname, QueryType.TXT);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hostname"></param>
        /// <returns></returns>
        public static IDnsQueryResponse GetARecords(string hostname)
        {
            Guard.Argument(hostname, nameof(hostname)).NotNull().NotEmpty().NotWhiteSpace();
            return Query(hostname, QueryType.A);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hostname"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        private static IDnsQueryResponse Query(string hostname, QueryType type)
        {
            Guard.Argument(hostname, nameof(hostname)).NotNull().NotEmpty().NotWhiteSpace();
            var endpoint = new IPEndPoint(IPAddress.Parse("9.9.9.9"), 53); //@TODO get these from settings
            var client = new LookupClient(endpoint);
            return client.Query(hostname, type);
        }
    }
}