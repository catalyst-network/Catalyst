public class NSecKey : IKey{
    private NSec.Cryptography.Key _key;

    public NSecKey(NSec.Cryptography.Key key){
        _key=key;
    }
    public NSec.Cryptography.Key GetNSecFormatKey()
    {
        return _key;
    }
} 