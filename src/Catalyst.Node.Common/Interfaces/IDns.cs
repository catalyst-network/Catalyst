using System.Collections.Generic;
using System.Threading.Tasks;
using DnsClient;

namespace Catalyst.Node.Common.Interfaces
{
    public interface IDns
    {
        /// <summary>
        /// </summary>
        /// <param name="hostnames"></param>
        /// <returns></returns>
        Task<IList<IDnsQueryResponse>> GetTxtRecords(IList<string> hostnames);

        /// <summary>
        /// </summary>
        /// <param name="hostname"></param>
        /// <returns></returns>
        Task<IDnsQueryResponse> GetTxtRecords(string hostname);
    }
}