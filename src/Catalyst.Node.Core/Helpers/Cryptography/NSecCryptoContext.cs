using NSec.Cryptography;

public class NSecCryptoContext : ICryptoContext{

    private static SignatureAlgorithm algorithm = SignatureAlgorithm.Ed25519;

    public IKey GenerateKey(){
        NSec.Cryptography.Key key = Key.Create(algorithm);
        return new NSecKey(key);
    }


    public void Sign (IKey key)
    {
        NSec.Cryptography.Key realKey = key.GetNSecFormatKey();
        algorithm.Sign(realKey, new System.ReadOnlySpan<byte>());
    }
    public void Sign(NSecKey key){
        
    }
}