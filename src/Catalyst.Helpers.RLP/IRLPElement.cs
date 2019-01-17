namespace Catalyst.Helpers.RLP
{
    /// <summary>
    ///     Wrapper class for decoded elements from an Catalyst.Helpers.RLP encoded byte array.
    /// </summary>
    public interface IRlpElement
    {
        byte[] RlpData { get; }
    }
}