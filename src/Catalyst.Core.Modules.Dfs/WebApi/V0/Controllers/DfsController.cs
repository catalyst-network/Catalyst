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

using System.IO;
using System.Text;
using System.Threading;
using Catalyst.Abstractions.Dfs.CoreApi;
using Catalyst.Core.Modules.Dfs.WebApi.V0.Filter;
using Catalyst.Core.Modules.Dfs.WebApi.V0.Response;
using Lib.P2P;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;

namespace Catalyst.Core.Modules.Dfs.WebApi.V0.Controllers
{
    /// <summary>
    ///   A base controller for Dfs HTTP API.
    /// </summary>
    /// <remarks>
    ///   Any unhandled exceptions are translated into an <see cref="ApiError"/> by the
    ///   <see cref="ApiExceptionFilter"/>.
    /// </remarks>
    [Route("api/v0")]
    [Produces("application/json")]
    [ApiExceptionFilter]
    public abstract class DfsController : Controller
    {
        /// <summary>
        ///   Creates a new instance of the controller.
        /// </summary>
        /// <param name="dfs">
        ///   An implementation of the Dfs Core API.
        /// </param>
        protected DfsController(ICoreApi dfs) { DfsService = dfs; }

        /// <summary>
        ///   An implementation of the Dfs Core API.
        /// </summary>
        protected ICoreApi DfsService { get; }

        /// <summary>
        ///   Notifies when the request is cancelled.
        /// </summary>
        /// <value>
        ///   See <see cref="Microsoft.AspNetCore.Http.HttpContext.RequestAborted"/>
        /// </value>
        /// <remarks>
        ///   There is no timeout for a request, because of the 
        ///   distributed nature of Dfs.
        /// </remarks>
        protected CancellationToken Cancel => HttpContext.RequestAborted;

        /// <summary>
        ///   Declare that the response is immutable and should be cached forever.
        /// @TODO lets look if there is any type of response interrceptor
        /// in the request lifecycle so we can force outgoing response to be immutable rather than adhock calling it
        /// </summary>
        protected void Immutable()
        {
            Response.Headers.Add("cache-control", new StringValues("public, max-age=31536000, immutable"));
        }

        /// <summary>
        ///   Get the strong ETag for a CID.
        /// </summary>
        protected EntityTagHeaderValue ETag(Cid id)
        {
            return new EntityTagHeaderValue(new StringSegment("\"" + id + "\""), isWeak: false);
        }

        /// <summary>
        ///   Immediately send the JSON.
        /// </summary>
        /// <param name="o">
        ///   The object to send to the requester.
        /// </param>
        /// <remarks>
        ///   Immediately sends the Line Delimited JSON (LDJSON) representation
        ///   of <paramref name="o"/> to the  requestor.
        /// </remarks>
        protected void StreamJson(object o)
        {
            if (!Response.HasStarted)
            {
                Response.StatusCode = 200;
                Response.ContentType = "application/json";
            }

            using (var sw = new StringWriter())
            {
                JsonSerializer.Create().Serialize(sw, o);
                sw.Write('\n');
                var bytes = Encoding.UTF8.GetBytes(sw.ToString());
                Response.Body.Write(bytes, 0, bytes.Length);
                Response.Body.Flush();
            }
        }
    }
}
