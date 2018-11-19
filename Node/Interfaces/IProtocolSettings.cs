namespace ADL.Node.Interfaces
{
    internal interface IProtocolSettings 
    {
        uint Magic { get; set; }
        byte AddressVersion { get; set; }
        string[] SeedList { get; set; }
    }
}