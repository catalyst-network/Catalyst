using System;
using System.Collections.Generic;
using System.Text;

namespace Catalyst.Common.Interfaces.Cli
{
    public interface ICliModule
    {
        bool HandleCommand(string[] args);
    }
}
