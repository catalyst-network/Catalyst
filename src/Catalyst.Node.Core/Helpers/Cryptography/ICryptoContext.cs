public interface ICryptoContext{

    IKey GenerateKey();
    byte[] Sign(IKey key);

    bool Verify(IKey key, );
}