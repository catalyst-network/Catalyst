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

using System;
using System.Threading.Tasks;
using Catalyst.Modules.Lib.Web3Api.Models;
using GraphQL;
using GraphQL.Types;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Serilog;

namespace Catalyst.Modules.Lib.Web3Api.Controllers
{
    public sealed class Web3Controller : Controller
    {
        private IDocumentExecuter DocumentExecutor { get; set; }
        private ISchema Schema { get; set; }
        private readonly ILogger _logger;

        public Web3Controller(IDocumentExecuter documentExecutor, ISchema schema, ILogger logger)
        {
            DocumentExecutor = documentExecutor;
            Schema = schema;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Web3Query query)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            var executionOptions = new ExecutionOptions
            {
                Schema = Schema, Query = query.Query
            };

            try
            {
                var result = await DocumentExecutor.ExecuteAsync(executionOptions).ConfigureAwait(false);

                if (result.Errors?.Count > 0)
                {
                    _logger.Debug("Web3Api errors: {0}", result.Errors);
                    return BadRequest(result);
                }

                _logger.Debug("Web3Api execution result: {result}", JsonConvert.SerializeObject(result.Data));
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.Debug("Document exexutor exception", ex);
                return BadRequest(ex);
            }
        }
    }
}
