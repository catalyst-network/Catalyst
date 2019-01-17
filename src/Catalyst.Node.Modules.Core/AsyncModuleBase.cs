using System.Threading;
using System.Threading.Tasks;

namespace Catalyst.Node.Modules.Core
{
    public abstract class AsyncModuleBase : ModuleBase
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