using Catalyst.Node.Core.Events;
using Catalyst.Node.Core.P2P;

namespace Catalyst.Node.Core {
    public interface ICatalystNode {
        ConnectionManager ConnectionManager { get; }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        void Announce(object sender, AnnounceNodeEventArgs e);
    }
}