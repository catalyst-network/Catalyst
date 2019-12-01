using Catalyst.Abstractions.IO.Events;
using Catalyst.Abstractions.KeySigner;
using Catalyst.Abstractions.P2P;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.IO.Messaging.Correlation;
using Catalyst.Modules.Server.Blazor.Models;
using Catalyst.Protocol.Cryptography;
using Catalyst.Protocol.Wire;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using Catalyst.Core.Lib.Util;
using Catalyst.Protocol.Transaction;

namespace Catalyst.Modules.Server.Blazor.Components
{
    public class SendTransactionFormBase : ComponentBase
    {
        public SendTransactionModel Model { get; set; } = new SendTransactionModel();

        [Inject]
        public IJSRuntime JsRuntime { get; set; }

        [Inject]
        public IKeySigner KeySigner { get; set; }

        [Inject]
        public IPeerSettings PeerSettings { get; set; }

        [Inject]
        public ITransactionReceivedEvent TransactionReceivedEvent { get; set; }

        public void HandleValidSubmit()
        {
            //TODO: Currently there is no method to actually set the transaction amount
            var transaction = new TransactionBroadcast {Timestamp = Timestamp.FromDateTime(DateTime.UtcNow)};
            transaction.PublicEntries.Add(new PublicEntry
            {
                Base = new BaseEntry
                {
                    SenderPublicKey = PeerSettings.PublicKey.KeyToByteString()
                }
            });
            var signingContext = new SigningContext
            {
                NetworkType = PeerSettings.NetworkType,
                SignatureType = SignatureType.ProtocolPeer
            };

            var signature = KeySigner.Sign(transaction.ToByteArray(), signingContext);
            transaction.Signature = new Signature
            {
                RawBytes = signature.SignatureBytes.ToByteString(),
                SigningContext = signingContext
            };
            var status = TransactionReceivedEvent
               .OnTransactionReceived(transaction.ToProtocolMessage(PeerSettings.PeerId,
                    CorrelationId.GenerateCorrelationId()));
            JsRuntime.InvokeAsync<object>("window.alert", $"Transaction Status: {status.ToString()}")
               .ConfigureAwait(false);
            Model = new SendTransactionModel();
            StateHasChanged();
        }
    }
}
