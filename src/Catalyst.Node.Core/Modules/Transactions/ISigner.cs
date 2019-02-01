namespace Catalyst.Node.Core.Modules.Transactions
{
    public interface ISigner
    {
        byte[] Sign(object certificate, byte[] hash, string hashOID);
        //  byte[] StripPrivateKey(object certificate);
    }
}