namespace Catalyst.Node.Common.Shell
{
    public interface IAds
    {
        /// <summary>
        /// </summary>
        /// <returns></returns>
        bool RunConsole();

        /// <summary>
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        bool OnRpcCommand(string[] args);

        /// <summary>
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        bool OnDfsCommand(string[] args);

        /// <summary>
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        bool OnPeerCommand(string[] args);

        /// <summary>
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        bool OnWalletCommand(string[] args);

        /// <summary>
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        bool OnGossipCommand(string[] args);

        /// <summary>
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        bool OnServiceCommand(string[] args);

        /// <summary>
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        bool OnConsensusCommand(string[] args);
    }
}