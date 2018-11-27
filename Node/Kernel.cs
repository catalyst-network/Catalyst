using Autofac;
using Akka.DI.Core;
using ADL.Node.Interfaces;

namespace ADL.Node
{
    internal sealed  class Kernel : IKernel
    {        
        
        private static Kernel _instance;
        public Settings Settings { get; set; }
        public IContainer Container { get; set; }
        public IDependencyResolver Resolver { get; set; }
        private static readonly object Mutex = new object();

        /// <summary>
        /// Get a thread safe kernel singleton.
        /// </summary>
        /// <param name="resolver"></param>
        /// <param name="settings"></param>
        /// <param name="container"></param>
        /// <returns></returns>
        public static Kernel GetInstance(Settings settings, IDependencyResolver resolver, IContainer container)
        { 
            if (_instance == null) 
            { 
                lock (Mutex)
                {
                    if (_instance == null) 
                    { 
                        _instance = new Kernel(settings, resolver, container);
                    }
                } 
            }
            return _instance;
        }
        
       /// <summary>
       /// Private kernel constructor.
       /// </summary>
       /// <param name="resolver"></param>
       /// <param name="settings"></param>
       /// <param name="container"></param>
        private Kernel(Settings settings, IDependencyResolver resolver, IContainer container)
        {
            Settings = settings;
            Resolver = resolver;
            Container = container;
        }
    }
}
