using Catalyst.Helpers.Logger;
using Autofac;
using Autofac.Core;

namespace Catalyst.Node.Modules.Core
{
    //@TODO make start stop abstract
    public abstract class ModuleBase : Module
    {
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public virtual bool StartService()
        {
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public virtual bool StopService()
        {
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public bool RestartService()
        {
            if (StopService())
            {
                if (StartService())
                {
                    Log.Message("service restarted successfully"); //@TODO get service name from class attribute
                }
                else
                {
                    Log.Message("Couldn't start service"); //@TODO get service name from class attribute
                    return false;
                }
            }
            else
            {
                Log.Message("Couldn't stop service"); //@TODO get service name from class attribute
                return false;
            }
            return true;
        }
    }
}
