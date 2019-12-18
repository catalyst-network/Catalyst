using System;

namespace Catalyst.Core.Modules.Dfs.WebApi.V0.Dto
{
    /// <summary>
    ///  A path to some data.
    /// </summary>
    public class PathDto
    {
        /// <summary>
        ///   Something like "/ipfs/QmYNQJoKGNHTpPxCBPh9KkDpaExgd2duMa3aF6ytMpHdao".
        /// </summary>
        public string Path;

        /// <summary>
        ///   Create a new path.
        /// </summary>
        /// <param name="path"></param>
        public PathDto(String path) { Path = path; }
    }
}
