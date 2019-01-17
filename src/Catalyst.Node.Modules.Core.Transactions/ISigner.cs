namespace Catalyst.Node.Modules.Core.Transactions
{
    public interface ISigner
    {
        byte[] Sign(object certificate, byte[] hash, string hashOID);
        //  byte[] StripPrivateKey(object certificate);
    }
}