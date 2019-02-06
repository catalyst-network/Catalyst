using System.Threading.Tasks;
using Akka.Actor;
using Catalyst.Node.Common.Modules;
using Catalyst.Node.Core.Helpers.Logger;
using Catalyst.Node.Core.Modules.Transactions;

namespace Catalyst.Node.Core.Modules.Gossip
{
    public class Gossip : UntypedActor, IGossip
    {
        private IActorRef GossipActor { get; set; }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        private async Task RunAsyncActors()
        {
            using (var gossipSystem = ActorSystem.Create("GossipSystem"))
            {
                GossipActor = gossipSystem.ActorOf(Props.Create(() => new Gossip()), "GossipActor");
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="message"></param>
        protected override void OnReceive(object message)
        {
            Log.Message("LocalPeerService OnReceive");

            Log.Message($"Message received {message}");
            switch (message)
            {
                case BasicTransaction tx:
                    switch (tx.BSending)
                    {
                        case true:
                            // Sending transaction
                            SendTransaction(tx);
                            break;
                        default:
                            ReceiveTransaction(tx);
                            break;
                    }

                    break;
            }

            var receivedMessage = (BasicTransaction) message;
        }

        /// <summary>
        /// </summary>
        private void SendTransaction()
        {
            SendTransaction(null);
        }

        /// <summary>
        /// </summary>
        /// <param name="tx"></param>
        private void SendTransaction(BasicTransaction tx)
        {
            // Send the message to connected nodes as an object so it's unpacked and cast
            // to the same Catalyst.Node.Modules.Core.Transactions object on the receiving end
        }

        /// <summary>
        /// </summary>
        /// <param name="tx"></param>
        private void ReceiveTransaction(BasicTransaction tx)
        {
            var p2pkh = new P2pkh();
            p2pkh.CheckInput(tx);
        }
    }
}