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
using Catalyst.Abstractions.Kvm.Models;
using Catalyst.Abstractions.Ledger;
using Nethermind.Core;
using IJsonSerializer = Nethermind.Core.IJsonSerializer;

namespace Catalyst.Core.Modules.Web3.Controllers.Handlers
{
    public abstract class EthWeb3RequestHandlerBase
    {
        public abstract int ParametersCount { get; }
        public abstract object Handle(object[] parameters, IWeb3EthApi api, IJsonSerializer serializer);

        [Todo(Improve.MissingFunctionality, "Implement BlockParametersConverter")]
        protected TParam Deserialize<TParam>(object parameter, IJsonSerializer serializer)
        {
            var parameterString = parameter is string ? $"\"{parameter}\"" : parameter.ToString();

            // use BlockParamConverter instead...
            if (typeof(TParam) == typeof(BlockParameter))
            {
                BlockParameter blockParameter = new BlockParameter();
                blockParameter.FromJson(parameterString);
                return (TParam) Convert.ChangeType(blockParameter, typeof(TParam));
            }

            return serializer.Deserialize<TParam>(parameterString);
        }
    }

    public abstract class EthWeb3RequestHandler<TResult> : EthWeb3RequestHandlerBase
    {
        public override int ParametersCount => 0;

        public override object Handle(object[] parameters, IWeb3EthApi api, IJsonSerializer serializer)
        {
            return Handle(api);
        }

        protected abstract TResult Handle(IWeb3EthApi api);
    }

    public abstract class EthWeb3RequestHandler<TParam1, TResult> : EthWeb3RequestHandlerBase
    {
        public override int ParametersCount => 1;

        public override object Handle(object[] parameters, IWeb3EthApi api, IJsonSerializer serializer)
        {
            TParam1 param1 = Deserialize<TParam1>(parameters[0], serializer);
            return Handle(param1, api);
        }

        protected abstract TResult Handle(TParam1 param1, IWeb3EthApi api);
    }

    public abstract class EthWeb3RequestHandler<TParam1, TParam2, TResult> : EthWeb3RequestHandlerBase
    {
        public override int ParametersCount => 2;

        public override object Handle(object[] parameters, IWeb3EthApi api, IJsonSerializer serializer)
        {
            TParam1 param1 = Deserialize<TParam1>(parameters[0], serializer);
            TParam2 param2 = Deserialize<TParam2>(parameters[1], serializer);
            return Handle(param1, param2, api);
        }

        protected abstract TResult Handle(TParam1 address, TParam2 param2, IWeb3EthApi api);
    }

    public abstract class EthWeb3RequestHandler<TParam1, TParam2, TParam3, TResult> : EthWeb3RequestHandlerBase
    {
        public override int ParametersCount => 3;

        public override object Handle(object[] parameters, IWeb3EthApi api, IJsonSerializer serializer)
        {
            TParam1 param1 = Deserialize<TParam1>(parameters[0], serializer);
            TParam2 param2 = Deserialize<TParam2>(parameters[1], serializer);
            TParam3 param3 = Deserialize<TParam3>(parameters[2], serializer);
            return Handle(param1, param2, param3, api);
        }

        protected abstract TResult Handle(TParam1 param1, TParam2 param2, TParam3 param3, IWeb3EthApi api);
    }
}
