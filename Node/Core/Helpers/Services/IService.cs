namespace ADL.Node.Core.Helpers.Services
{
    public interface IService<out T>
    {
        bool StartService();
        bool StopService();
        bool RestartService();
        T GetImpl();
    }
}
