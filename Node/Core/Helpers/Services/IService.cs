namespace ADL.Node.Core.Helpers.Services
{
    public interface IService
    {
        bool StartService();
        bool StopService();
        bool RestartService();
    }
}
