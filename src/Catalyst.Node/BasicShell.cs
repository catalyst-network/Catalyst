using Catalyst.Helpers.Shell;

namespace Catalyst.Node
{
    public class BasicShell : ShellBase
    {
        public bool Run()
        {
            return RunConsole();
        }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        /// <returns></returns>
        public override bool OnGetConfig()
        {
//            Log.Message(Catalyst.Kernel.Settings.SerializeSettings());
            return true;
        }

        /// /// </summary>
        /// <returns></returns>
        public override bool OnGetInfo()
        {
            return true;
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public override bool OnGetVersion()
        {
            return true;
        }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        public override bool OnStop(string[] args)
        {
//            Catalyst.Dispose();
            return false;
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public override bool OnStart(string[] args)
        {
            return true;
        }

        /// <summary>
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public override bool OnStartNode(string[] args)
        {
            return true;
        }

        /// <summary>
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public override bool OnStartWork(string[] args)
        {
            return true;
        }

        /// <summary>
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public override bool OnStopNode(string[] args)
        {
            return true;
        }

        /// <summary>
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public override bool OnStopWork(string[] args)
        {
            return true;
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public override bool OnGetMempool()
        {
            return true;
        }
    }
}
