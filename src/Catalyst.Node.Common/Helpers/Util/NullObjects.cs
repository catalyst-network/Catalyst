using System;
using System.Collections.Generic;
using System.Text;
using Catalyst.Node.Common.Helpers.IO.Inbound;
using Google.Protobuf.WellKnownTypes;

namespace Catalyst.Node.Common.Helpers.Util
{
    public static class NullObjects
    {
        public static readonly Any Any = new Any();
        //public static readonly ContextAny = new ContextAny(new Any(), (IChannel) null)
    }
}
