using System;
using System.Threading.Tasks;

namespace Catalyst.Node.Common.Interfaces
{
    public interface IISocket: IComparable<IISocket>
    {
        Task Shutdown();
    }
}