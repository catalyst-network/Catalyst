namespace ADL.RLP
{
    /// <summary>
    ///     Wrapper class for decoded elements from an RLP encoded byte array.
    /// </summary>
    public interface IRlpElement
    {
        byte[] RlpData { get; }
    }
}