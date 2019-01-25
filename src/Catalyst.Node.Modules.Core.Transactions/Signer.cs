using System;

namespace Catalyst.Node.Modules.Core.Transactions
{
    public class WindowsSigner : ISigner
    {
        public byte[] Sign(object certificate, byte[] hash, string hashOID)
        {
            throw new NotImplementedException();
        }
    }
}
