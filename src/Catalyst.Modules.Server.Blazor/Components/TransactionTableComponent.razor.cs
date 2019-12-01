using System;
using System.Linq;
using System.Threading;
using Catalyst.Core.Lib.DAO;
using Microsoft.AspNetCore.Components;

namespace Catalyst.Modules.Server.Blazor.Components
{
    public class TransactionTableComponentBase : ComponentBase
    {
        [Parameter] public TransactionBroadcastDao Model { get; set; }

        public string GetAmount() { return Model.PublicEntries.First().Amount; }
    }
}
