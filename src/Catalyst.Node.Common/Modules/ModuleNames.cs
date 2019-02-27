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
        protected ModuleNames() {}

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

        public static readonly string Consensus = "Consensus";
        public static readonly string Contract = "Contract";
        public static readonly string Dfs = "Dfs";
        public static readonly string Gossip = "Gossip";
        public static readonly string Ledger = "Ledger";
        public static readonly string Mempool = "Mempool";

        private static Lazy<IList<string>> _publicConstantStringsFromThisClass;
        public static IList<string> All => _publicConstantStringsFromThisClass.Value;
    }
}
