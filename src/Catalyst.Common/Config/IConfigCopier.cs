namespace Catalyst.Common.Config
{
    public interface IConfigCopier
    {
        /// <summary>
        ///     Finds out which config files are missing from the catalyst home directory and
        ///     copies them over if needed.
        /// </summary>
        /// <param name="dataDir">Home catalyst directory</param>
        /// <param name="network">Network on which to run the node</param>
        /// <param name="sourceFolder"></param>
        /// <param name="overwrite">Should config existing config files be overwritten by default?</param>
        void RunConfigStartUp(string dataDir, Network network, string sourceFolder = null, bool overwrite = false);
    }
}
