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
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Abstractions.Consensus.Deltas;
using Catalyst.Abstractions.Dfs;
using Catalyst.Core.Lib.DAO;
using Catalyst.Core.Lib.DAO.Deltas;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Protocol.Deltas;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace Catalyst.Core.Modules.Web3.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public sealed class LedgerController : Controller
    {
        private readonly IDeltaHashProvider _deltaHashProvider;
        private readonly IDfsService _dfsService;
        private readonly IMapperProvider _mapperProvider;
        private readonly ILogger _logger;

        public LedgerController(IDeltaHashProvider deltaHashProvider, IDfsService dfsService, IMapperProvider mapperProvider, ILogger logger)
        {
            _deltaHashProvider = deltaHashProvider;
            _dfsService = dfsService;
            _mapperProvider = mapperProvider;
            _logger = logger;
        }

        [HttpGet]
        public async Task<JsonResult> GetLatestDeltaAsync(DateTime? asOf)
        {
            var latest = _deltaHashProvider.GetLatestDeltaHash(asOf?.ToUniversalTime());
            try
            {
                await using (var fullContentStream = await _dfsService.UnixFsApi.ReadFileAsync(latest).ConfigureAwait(false))
                {
                    var contentBytes = await fullContentStream.ReadAllBytesAsync(CancellationToken.None)
                       .ConfigureAwait(false);
                    var delta = Delta.Parser.ParseFrom(contentBytes).ToDao<Delta, DeltaDao>(_mapperProvider);

                    return Json(new
                    {
                        Success = true,
                        DeltaHash = latest,
                        Delta = delta
                    });
                }
            }
            catch (Exception e)
            {
                _logger.Error(e, "Failed to find dfs content for delta as of {asOf} at {dfsTarget}", asOf, latest);
                return Json(new
                {
                    Success = false,
                    Message = $"Failed to find dfs content for delta as of {asOf} at {latest}"
                });
            }
        }
    }
}
