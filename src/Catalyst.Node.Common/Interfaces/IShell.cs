namespace Catalyst.Node.Common.Interfaces
{
    public interface IShell
    {
        /// <summary>
        /// </summary>
        /// <returns></returns>
        bool OnStart(string[] args);

        /// <summary>
        /// </summary>
        /// <returns></returns>
        bool OnStartNode(string[] args);

        /// <summary>
        /// </summary>
        /// <returns></returns>
        bool OnStartWork(string[] args);

        /// <summary>
        /// </summary>
        bool OnStop(string[] args);

        /// <summary>
        /// </summary>
        /// <returns></returns>
        bool OnStopNode(string[] args);

        /// <summary>
        /// </summary>
        /// <returns></returns>
        bool OnStopWork(string[] args);
    }
}