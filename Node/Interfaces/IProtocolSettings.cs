namespace ADL.Node.Interfaces
{
    public interface IProtocolSettings 
    {
        uint Magic { get; set; }
        string[] SeedList { get; set; }
        byte AddressVersion { get; set; }
    }
}