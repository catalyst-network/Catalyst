using System.Threading;
using System.Threading.Tasks;

namespace ADL.Node.Core.Helpers.Services
{
    public abstract class AsyncServiceBase : ServiceBase
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public Task AwaitCancellation(CancellationToken token = new CancellationToken())
        {
            var taskSource = new TaskCompletionSource<bool>();
            token.Register(() => taskSource.SetResult(true));
            return taskSource.Task;
        }
    }
}