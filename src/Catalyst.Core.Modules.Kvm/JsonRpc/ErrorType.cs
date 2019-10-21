using System;
using System.Collections.Generic;
using System.Text;

namespace Catalyst.Core.Modules.Kvm.JsonRpc
{
    public enum ErrorType
    {
        None,
        ParseError,
        InvalidRequest,
        MethodNotFound,
        InvalidParams,
        InternalError,
        ExecutionError,
        ServerError,
        NotFound
    }
}
}
