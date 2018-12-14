namespace ADL.Node.Core.Helpers.Services
{
    public interface IService<T>
    {
        bool StartService();
        bool StopService();
        bool RestartService();
        T GetImpl();
    }
}
