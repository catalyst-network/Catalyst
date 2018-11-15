using System;
using System.Collections;
using Akka.Actor;
using ADL.Transaction;

namespace ADL.LocalPeer
{
    public class LocalPeerActor : UntypedActor
    {
        protected override void PreStart() => Console.WriteLine("Started actor");
    
        protected override void PostStop() => Console.WriteLine("Stopped actor");
    
        protected override void OnReceive(object message)
        {
            Console.WriteLine($"Message received {message}");

            switch (message)
            {
                case BasicTransaction tx:
                    switch (tx.bSending)
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
                
                default:
                    break;
            }
            
            
            var receivedMessage = (BasicTransaction) message;
        }
        
        
        private void SendTransaction(BasicTransaction tx)
        {
            // Send the message to connected nodes as an object so it's unpacked and cast
            // to the same Transaction object on the receiving end
        }
        
        private void ReceiveTransaction(BasicTransaction tx)
        {
            P2pkh p2pkh = new P2pkh();
            p2pkh.CheckInput(tx);
        }
    }
}
