using System;
using Akka.Actor;

namespace ADL.TaskManager
{
    public class TaskManagerService : UntypedActor, ITaskManagerService
    {
        protected override void PreStart() => Console.WriteLine("Started TaskManagerService actor");

        protected override void PostStop() => Console.WriteLine("Stopped TaskManagerService actor");

        protected override void OnReceive(object message)
        {
            Console.WriteLine("TaskManagerService OnReceive");

            Console.WriteLine($"Message received {message}");
        }
    }
}
