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
using System.Linq;
using Catalyst.Core.Lib.P2P.Repository;
using Catalyst.Core.Lib.Util;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using NLog.StructuredLogging.Json.Helpers;
using SimpleBase;

namespace Catalyst.Core.Modules.Web3.Controllers
{
    public class ByteArrayHexConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => objectType == typeof(byte[]);

        public override bool CanRead => false;
        public override bool CanWrite => true;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) => throw new NotImplementedException();

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var base58String = Base58.Bitcoin.Encode((byte[]) value);
            writer.WriteValue(base58String);
        }
    }

    [ApiController]
    [Route("api/[controller]/[action]")]
    public sealed class PeerController : Controller
    {
        private readonly IPeerRepository _peerRepository;

        public PeerController(IPeerRepository peerRepository)
        {
            _peerRepository = peerRepository;
        }

        // GET: api/values
        [HttpGet]
        public JsonResult GetAllPeers()
        {
            var peers = _peerRepository.GetAll().ToList();
            //peers.ForEach(x =>
            //{
            //    x.PeerIdentifier.PublicKey = Base58.Bitcoin.Encode(x.PeerIdentifier.PublicKey);
            //});

            //var jsonResult = Json(_peerRepository.GetAll(), new JsonSerializerSettings
            //{
            //    Converters = JsonConverterProviders.Converters.ToList()
            //});

            var converters = JsonConverterProviders.Converters.ToList();
            converters.Add(new ByteArrayHexConverter());

            return Json(_peerRepository.GetAll(), new JsonSerializerSettings
            {
                Converters = converters
            });
        }
    }
}
