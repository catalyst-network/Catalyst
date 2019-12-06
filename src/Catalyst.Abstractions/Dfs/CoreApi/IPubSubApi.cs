using Lib.P2P.PubSub;

namespace Catalyst.Abstractions.Dfs.CoreApi
{
    /// <summary>
    ///   Allows you to publish messages to a given topic, and also to
    ///   subscribe to new messages on a given topic.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///   This is an experimental feature. It is not intended in its current state
    ///   to be used in a production environment.
    ///   </para>
    ///   <para>
    ///   To use, the daemon must be run with '--enable-pubsub-experiment'.
    ///   </para>
    /// </remarks>
    /// <seealso href="https://github.com/ipfs/interface-ipfs-core/blob/master/SPEC/PUBSUB.md">Pubsub API spec</seealso>
    public interface IPubSubApi : IPubSub { }
}
