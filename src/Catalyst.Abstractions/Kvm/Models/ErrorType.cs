namespace Catalyst.Abstractions.Kvm.Models
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
