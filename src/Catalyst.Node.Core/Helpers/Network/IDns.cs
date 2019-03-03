using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using DnsClient;

namespace Catalyst.Node.Core.Helpers.Network
{
    public interface IDns
    {
        IPEndPoint DnsServer { get; set; }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="hostnames"></param>
        /// <returns></returns>
        Task<IList<IDnsQueryResponse>> GetTxtRecords(List<string> hostnames);
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="hostname"></param>
        /// <returns></returns>
        Task<IDnsQueryResponse> GetTxtRecords(string hostname);
    }
}