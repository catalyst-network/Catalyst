#region LICENSE

/**
* Copyright (c) 2019 Catalyst Network
*
* This file is part of Catalyst.Node <https://github.com/catalyst-network/Catalyst.Node>
*
* Catalyst.Node is free software: you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation, either version 2 of the License, or
* (at your option) any later version.
*
* Catalyst.Node is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
* GNU General Public License for more details.
*
* You should have received a copy of the GNU General Public License
* along with Catalyst.Node. If not, see <https://www.gnu.org/licenses/>.
*/

#endregion

using Nethermind.Core.Model;

namespace Catalyst.Abstractions.Kvm.Models
{
    public class ResultWrapper<T>
    {
        public T Data { get; set; }
        public Result Result { get; set; }
        public ErrorType ErrorType { get; set; }

        private ResultWrapper() { }

        public static ResultWrapper<T> Fail(string error)
        {
            return new ResultWrapper<T>
            {
                Result = Result.Fail(error), ErrorType = ErrorType.InternalError
            };
        }

        public static ResultWrapper<T> Fail(string error, ErrorType errorType, T outputData)
        {
            return new ResultWrapper<T>
            {
                Result = Result.Fail(error), ErrorType = errorType, Data = outputData
            };
        }

        public static ResultWrapper<T> Fail(string error, ErrorType errorType)
        {
            return new ResultWrapper<T>
            {
                Result = Result.Fail(error), ErrorType = errorType
            };
        }

        public static ResultWrapper<T> Success(T data)
        {
            return new ResultWrapper<T>
            {
                Data = data, Result = Result.Success
            };
        }

        public Result GetResult()
        {
            return Result;
        }

        public object GetData()
        {
            return Data;
        }

        public ErrorType GetErrorType()
        {
            return ErrorType;
        }
    }
}
