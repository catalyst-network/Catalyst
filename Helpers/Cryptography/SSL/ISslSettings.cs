namespace ADL.Cryptography.SSL
{
    public interface ISslSettings 
    {
        string PfxFileName { get; set; }
        string SslCertPassword { get; set; }
    }
}