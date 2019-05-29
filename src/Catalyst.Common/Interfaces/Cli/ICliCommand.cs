using System;
using System.Collections.Generic;
using System.Text;

namespace Catalyst.Common.Interfaces.Cli
{
    public interface ICliCommand
    {
        bool HandleCommand(object args);
    }
}
