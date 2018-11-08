using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ADL.Helpers
{
    enum Caller{Local=1, RPC};

    internal  class Env
    {
        private static IConfigurationSection _section = null;
        private static uint _edges;
        private static uint _port;
        private static string _homeDir;
        private static string _host;
        private static uint _secondsPerBlock;
        private static List<string> _remotePeers;

        private static IConfigurationSection Section
        {
            get
            {
                if (_section == null)
                {
                    _section = new ConfigurationBuilder().AddJsonFile("Protocol.json").Build().GetSection("ProtocolConfiguration");
                }

                return _section;
            }
        }
        
        internal static bool Daemon
        {
            get;
            set;
        }

        internal static bool Sysop
        {
            get;
            set;
        }

        internal static uint Edges 
        {
            get
            {
                if (_edges == 0)
                {
                    _edges = GetValueOrDefault(Section.GetSection("EdgeLimit"), (uint)5, p => uint.Parse(p));
                }

                return _edges;
            }
            set
            {
                _edges = value;
            }
        }

        internal static uint Port
        {
            get
            {
                if (_port == 0)
                {
                    _port = GetValueOrDefault(Section.GetSection("Port"), (uint)38869, p => uint.Parse(p));
                }

                return _port;
            }
            set
            {
                _port = value;
            }
        }

        internal static string HomeDir
        {
            get
            {
                if (string.IsNullOrEmpty(_homeDir))
                {
                    _homeDir = GetValueOrDefault(Section.GetSection("HomeDir"), "~", p => p);
                }

                return _homeDir;
            }
            set
            {
                _homeDir = value;
            }
        }

        internal static string Host
        {
            get
            {
                if (string.IsNullOrEmpty(_host))
                {
                    _host = GetValueOrDefault(Section.GetSection("Host"), "127.0.0.1", p => p);
                }

                return _host;
            }
            set
            {
                _host = value;
            }
        }

        internal static uint SecondsPerBlock
        {
            get
            {
                if (_secondsPerBlock == 0)
                {
                    _secondsPerBlock = GetValueOrDefault(Section.GetSection("SecondsPerBlock"), (uint)60, p => uint.Parse(p));
                }

                return _secondsPerBlock;
            }
            set
            {
                _secondsPerBlock = value;
            }
        }

        internal static List<string> RemotePeers
        {
            get
            {
                if (_remotePeers == null)
                {
                   _remotePeers = Section.GetSection("SeedList").GetChildren().Select(p => p.Value).ToList();
                }

                return _remotePeers;
            }
            set
            {
                _remotePeers = value;
            }
        }
        
        private static T GetValueOrDefault<T>(IConfigurationSection section, T defaultValue, Func<string, T> selector)
        {
            if (section.Value == null) return defaultValue;
            return selector(section.Value);
        }
    }
}
