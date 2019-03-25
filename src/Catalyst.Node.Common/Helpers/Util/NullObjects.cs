using Catalyst.Node.Common.Helpers.IO.Inbound;
using Google.Protobuf.WellKnownTypes;

namespace Catalyst.Node.Common.Helpers.Util {
    public static class NullObjects
    {
        public static readonly Any Any = new Any();
        public static readonly ChanneledAny ChanneledAny = new ChanneledAny(null, new Any());
    }
}