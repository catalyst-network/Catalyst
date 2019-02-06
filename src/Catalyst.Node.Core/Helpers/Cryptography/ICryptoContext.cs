public interface ICryptoContext{

    IKey GenerateKey();
    void Sign(IKey key);
}