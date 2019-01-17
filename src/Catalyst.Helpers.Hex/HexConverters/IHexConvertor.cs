namespace Catalyst.Helpers.Hex.HexConverters
{
    public interface IHexConvertor<T>
    {
        string ConvertToHex(T value);
        T ConvertFromHex(string value);
    }
}