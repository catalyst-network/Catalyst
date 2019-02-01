using System;

namespace Catalyst.Node.Core.Modules.Transactions
{
    public class WindowsSigner : ISigner
    {
        public byte[] Sign(object certificate, byte[] hash, string hashOID)
        {
            throw new NotImplementedException();
        }
    }
}
