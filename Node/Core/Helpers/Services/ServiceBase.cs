using System;

namespace ADL.Node.Core.Helpers.Services
{
    abstract public class ServiceBase
    {
        public bool StartService()
        {
            throw new System.NotImplementedException();
        }

        public bool StopService()
        {
            throw new System.NotImplementedException();
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