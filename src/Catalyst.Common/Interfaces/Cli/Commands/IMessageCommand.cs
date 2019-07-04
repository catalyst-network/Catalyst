using System;
using System.Collections.Generic;
using System.Text;
using Catalyst.Common.Interfaces.Rpc;
using Google.Protobuf;

namespace Catalyst.Common.Interfaces.Cli.Commands
{
    public interface IMessageCommand<out T> : ICommand
        where T : IMessage<T>
    {
        /// <summary>The node to send the message to.</summary>
        /// <value>The target node.</value>
        INodeRpcClient Target { get; }
    }
}
