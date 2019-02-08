using NSec.Cryptography;
using System;

public class NSecCryptoContext : ICryptoContext{

    private static SignatureAlgorithm algorithm = SignatureAlgorithm.Ed25519;

    public IKey GenerateKey(){
        NSec.Cryptography.Key key = NSec.Cryptography.Key.Create(algorithm);
        return new NSecKey(key);
    }


    public byte[] Sign (IKey key, )
    {
        NSec.Cryptography.Key realKey = key.GetNSecFormatKey();
        return Sign(realKey);
        
    }
    private byte[] Sign(NSec.Cryptography.Key realKey, ReadOnlySpan<byte>){
        return algorithm.Sign(realKey, new ReadOnlySpan<byte>());
    }
}