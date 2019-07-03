using System;
using System.Collections.Generic;
using System.Text;
using Catalyst.Common.Interfaces.Cli.Options;
using Catalyst.Common.Interfaces.Rpc;
using Google.Protobuf;

namespace Catalyst.Common.Interfaces.Cli.Commands
{
    public interface ICommandBase<out T, in TOption> 
        where T : IMessage<T>
        where TOption : IOptionsBase
    {
        bool SendMessage(TOption options);

        T GetMessage(TOption options);

        INodeRpcClient Target { get; }

        Type GetOptionType();
    }
}
