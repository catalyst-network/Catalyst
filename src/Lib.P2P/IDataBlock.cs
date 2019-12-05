using System.IO;

namespace Lib.P2P
{
    /// <summary>
    ///   represents some data dfs
    /// </summary>
    /// <remarks>
    ///   A <b>DataBlock</b> has an <see cref="Id">unique ID</see>
    ///   and some data (<see cref="IDataBlock.DataBytes"/> 
    ///   or <see cref="IDataBlock.DataStream"/>).
    ///   <para>
    ///   It is useful to talk about them as "blocks" in Bitswap 
    ///   and other things that do not care about what is being stored.
    ///   </para>
    /// </remarks>
    /// <seealso>
    ///     <cref>Catalyst.Ipfs.Core.IMerkleNode{Link}</cref>
    /// </seealso>
    public interface IDataBlock
    {
        /// <summary>
        ///   Contents as a byte array.
        /// </summary>
        /// <remarks>
        ///   It is never <b>null</b>.
        /// </remarks>
        /// <value>
        ///   The contents as a sequence of bytes.
        /// </value>
        byte[] DataBytes { get; }

        /// <summary>
        ///   Contents as a stream of bytes.
        /// </summary>
        /// <value>
        ///   The contents as a stream of bytes.
        /// </value>
        Stream DataStream { get; }

        /// <summary>
        ///   The unique ID of the data.
        /// </summary>
        /// <value>
        ///   A <see cref="Lib.P2P.Cid"/> of the content.
        /// </value>
        Cid Id { get; }

        /// <summary>
        ///   The size (in bytes) of the data.
        /// </summary>
        /// <value>Number of bytes.</value>
        long Size { get; }
    }
}
