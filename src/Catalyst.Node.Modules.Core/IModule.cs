namespace Catalyst.Node.Modules.Core
{
    /// <summary>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IModule<out T>
    {
        /// <summary>
        /// </summary>
        /// <returns></returns>
        bool StartService();

        /// <summary>
        /// </summary>
        /// <returns></returns>
        bool StopService();

        /// <summary>
        /// </summary>
        /// <returns></returns>
        bool RestartService();

        /// <summary>
        /// </summary>
        /// <returns></returns>
        T GetImpl();
    }
}