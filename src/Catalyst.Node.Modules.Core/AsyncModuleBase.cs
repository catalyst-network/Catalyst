using System.Threading;
using System.Threading.Tasks;
using Autofac;

namespace Catalyst.Node.Modules.Core
{
    public abstract class AsyncModuleBase : Module
    {
        /// <summary>
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