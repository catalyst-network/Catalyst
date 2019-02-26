using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

namespace Catalyst.Node.Common.Modules
{
    public class ModuleNames
    {
        static ModuleNames()
        {
            _publicConstantStringsFromThisClass = new Lazy<IList<string>>(() =>
            {
                var inheritedPublicStatic = BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy;
                var moduleNames = typeof(ModuleNames)
                   .GetFields(inheritedPublicStatic).Where(f => f.FieldType == typeof(string))
                   .Select(f => f.GetValue(null).ToString()).ToList();
                return moduleNames;
            }, LazyThreadSafetyMode.PublicationOnly);
        }

        public const string Consensus = "Consensus";
        public const string Contract = "Contract";
        public const string Dfs = "Dfs";
        public const string Gossip = "Gossip";
        public const string Ledger = "Ledger";
        public const string Mempool = "Mempool";

        private static Lazy<IList<string>> _publicConstantStringsFromThisClass;
        public static IList<string> All => _publicConstantStringsFromThisClass.Value;
    }
}
