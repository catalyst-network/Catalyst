namespace ADL.Services
{
    public interface IService
    {
        bool StartService();
        bool StopService();
        bool RestartService();
    }
}
