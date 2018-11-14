using System;
using Akka.Actor;

namespace ADL.DFS
{
    public class DfsActor : UntypedActor
    {
        private readonly IpfsWrapper _ipfsWrapper = new IpfsWrapper();

        public class ReadFile
        {
            public ReadFile(string hash)
            {
                Hash = hash;
            }
        
            public string Hash { get; }               
        }
        
        public class AddFile
        {
            public AddFile(string path)
            {
                Path = path; 
            }

            public string Path { get; }
        }
        
        protected override void OnReceive(object message)
        {
            switch (message)
            {
                case ReadFile read:
                    var readFileAsync = _ipfsWrapper.ReadFileAsync(read.Hash);
                    Sender.Tell(readFileAsync.Result, Self);
                    break;                
                case AddFile add:
                    try
                    {
                        var addFileAsync = _ipfsWrapper.AddFileAsync(add.Path);
                        Sender.Tell(addFileAsync.Result, Self);
                    }
                    catch (Exception e)
                    {
                        Sender.Tell(new Failure {Exception = e}, Self);
                    }
                    break;                
                default:
                    Console.WriteLine("Unknown Protocol");
                    break;
            }
        }
    }
}
