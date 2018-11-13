using System;
using System.IO;
using Akka.Actor;
using Akka.DI.Core;

namespace ADL.Cli.Interfaces
{
    public interface ICliApplication
    {       
        void Main(string[] args);
        void RegisterServices();
        IDependencyResolver BuildContainer(ActorSystem actorSystem);
        void PrintErrorLogs(StreamWriter writer, Exception ex);
        void UnhandledException(object sender, UnhandledExceptionEventArgs e);
    }
}