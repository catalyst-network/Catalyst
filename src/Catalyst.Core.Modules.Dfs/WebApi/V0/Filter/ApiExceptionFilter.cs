#region LICENSE

/**
* Copyright (c) 2024 Catalyst Network
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

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Catalyst.Core.Modules.Dfs.WebApi.V0.Response;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Catalyst.Core.Modules.Dfs.WebApi.V0.Filter
{
    /// <summary>
    ///     Handles exceptions thrown by a controller.
    /// </summary>
    /// <remarks>
    ///     Returns a <see cref="ApiError" /> to the caller.
    /// </remarks>
    internal sealed class ApiExceptionFilter : ExceptionFilterAttribute
    {
        /// <inheritdoc />
        public override void OnException(ExceptionContext context)
        {
            var statusCode = 500; // Internal Server Error
            var message = context.Exception.Message;
            string[] details = null;

            switch (context.Exception)
            {
                // Map special exceptions to a status code.
                case FormatException _:
                // Bad Request
                case KeyNotFoundException _:
                    statusCode = 400; // Bad Request
                    break;
                case TaskCanceledException _:
                    statusCode = 504; // Gateway Timeout
                    message = "The request took too long to process or was cancelled.";
                    break;
                case NotImplementedException _:
                    statusCode = 501; // Not Implemented
                    break;
                case TargetInvocationException _:
                    message = context.Exception.InnerException?.Message;
                    break;
            }

            // Internal Server Error or Not Implemented get a stack dump.
            if (statusCode == 500 || statusCode == 501)
            {
                details = context.Exception.StackTrace.Split(Environment.NewLine);
            }

            context.HttpContext.Response.StatusCode = statusCode;
            context.Result = new JsonResult(new ApiError
            {
                Message = message,
                Details = details
            });

            // Remove any caching headers
            context.HttpContext.Response.Headers.Remove("cache-control");
            context.HttpContext.Response.Headers.Remove("etag");
            context.HttpContext.Response.Headers.Remove("last-modified");

            base.OnException(context);
        }
    }
}
