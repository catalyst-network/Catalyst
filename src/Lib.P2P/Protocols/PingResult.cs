using System;

namespace Lib.P2P.Protocols
{
    /// <summary>
    ///   The result from sending a <see>
    ///       <cref>Catalyst.Ipfs.Core.CoreApi.IGenericApi.PingAsync(MultiFormats.MultiHash,int,System.Threading.CancellationToken)</cref>
    ///   </see>
    ///   .
    /// </summary>
    public class PingResult
    {
        /// <summary>
        ///   Indicates success or failure.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        ///   The round trip time; nano second resolution.
        /// </summary>
        public TimeSpan Time { get; set; }

        /// <summary>
        ///   The text to echo.
        /// </summary>
        public string Text { get; set; }
    }
}
