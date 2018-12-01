namespace ADL.Rpc
{
    public interface IRpcSettings
    {
        ushort Port { get; set; }
        string BindAddress { get; set; }
    }
}
