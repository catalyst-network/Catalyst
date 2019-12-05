namespace Lib.P2P.Transports
{
    /// <summary>
    ///   The statistics for <see>
    ///       <cref>Catalyst.Ipfs.Core.CoreApi.IStatsApi.BandwidthAsync(System.Threading.CancellationToken)</cref>
    ///   </see>
    ///   .
    /// </summary>
    public class BandwidthData
    {
        /// <summary>
        ///   The number of bytes received.
        /// </summary>
        public ulong TotalIn;

        /// <summary>
        ///   The number of bytes sent.
        /// </summary>
        public ulong TotalOut;

        /// <summary>
        ///   TODO
        /// </summary>
        public double RateIn;

        /// <summary>
        ///   TODO
        /// </summary>
        public double RateOut;
    }
}
