﻿using System;
using System.Threading;
using System.Threading.Tasks;
using ADL.Services;
 
 namespace ADL.Ledger
{
    public class LedgerService : AsyncServiceBase, ILedgerService
    {
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="settings"></param>
        public LedgerService()
        {
            
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="settings"></param>
        public bool StartService()
        {
            return true;
        }
        
        /// <summary>
        /// 
        /// </summary>
        public bool StopService()
        {
            return false;
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
