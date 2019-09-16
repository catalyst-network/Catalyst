using System;
using System.Collections.Generic;
using System.Text;

namespace Catalyst.Abstractions.Hashing
{
    public interface IHashProvider
    {
        string ComputeHash(byte[] content);
    }
}
