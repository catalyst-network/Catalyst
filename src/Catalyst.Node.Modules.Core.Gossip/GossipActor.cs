using Akka.Actor;
using Catalyst.Helpers.Logger;
using Catalyst.Node.Modules.Core.Transactions;

namespace Catalyst.Node.Modules.Core.Gossip
{
    public class GossipActor : UntypedActor, IGossip
    {
        protected override void PreStart()
        {
            Log.Message("Started Gossip actor");
        }

        protected override void PostStop()
        {
            Log.Message("Stopped Gossip actor");
        }

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


        private void SendTransaction()
        {
            SendTransaction(null);
        }

        private void SendTransaction(BasicTransaction tx)
        {
            // Send the message to connected nodes as an object so it's unpacked and cast
            // to the same Catalyst.Node.Modules.Core.Transactions object on the receiving end
        }

        private void ReceiveTransaction(BasicTransaction tx)
        {
            var p2pkh = new P2pkh();
            p2pkh.CheckInput(tx);
        }
    }
}