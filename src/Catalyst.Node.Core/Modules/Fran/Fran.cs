using Catalyst.Node.Common.Interfaces;
using Catalyst.Node.Common.Interfaces.Modules.Fran;
using Serilog;

namespace Catalyst.Node.Core.Modules.Fran
{
    public class Fran : IFran
    {

        
        public Fran(ILogger logger)
        {
            logger.Information("hi,,.,,");
            
            
        }
    }
}