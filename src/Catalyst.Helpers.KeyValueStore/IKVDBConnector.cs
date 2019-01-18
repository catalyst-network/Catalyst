using System.Net;

namespace Catalyst.Helpers.KeyValueStore
{
    public interface IKVDBConnector
    {
        IKVDBConnector GetInstance(IPAddress host);
    }
}