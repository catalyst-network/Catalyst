using System;

namespace ADL.Node.Core.Helpers.Services
{
    abstract public class ServiceBase
    {
        public virtual bool StartService()
        {
            return true;
        }

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
                    Console.WriteLine("RPC service restarted successfully");
                }
                else
                {
                    Console.WriteLine("Couldn't start rpc service");
                    return false;
                }
            }
            else
            {
                Console.WriteLine("Couldn't stop rpc service");
                return false;
            }
            return true;
        }
    }
}
