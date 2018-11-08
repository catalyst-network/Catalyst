using System;

namespace ADL.Helpers
{

    enum Caller{Local=1, RPC};

    public class Env
    {
        public static bool Daemon
        {
            get;
            set;
        }

        public static bool Shell
        {
            get;
            set;
        }

        public static uint Edges 
        {
            get;
            set;
        }

        public static uint Port
        {
            get;
            set;
        }

        public static string HomeDir
        {
            get;
            set;
        }

        public static string Host
        {
            get;
            set;
        }

        public static uint port
        {
            get;
            set;
        }
    }
}
