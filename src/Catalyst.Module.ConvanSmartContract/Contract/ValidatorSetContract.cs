#region LICENSE

/**
* Copyright (c) 2022 Catalyst Network
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

using Catalyst.Abstractions.Contract;
using Nethermind.Abi;
using Nethermind.Core;
using Nethermind.Serialization.Json.Abi;

namespace Catalyst.Module.ConvanSmartContract.Contract
{
    public class ValidatorSetContract : IValidatorSetContract
    {
        private readonly IAbiEncoder _abiEncoder;
        private readonly ICallableContractProxy _callableContractProxy;

        internal static readonly AbiDefinition Definition = new AbiDefinitionParser().Parse<ValidatorSet>();
        internal static readonly string GetValidatorsFunction = Definition.GetFunctionName(nameof(GetValidators));

        public ValidatorSetContract(IAbiEncoder abiEncoder, ICallableContractProxy callableContractProxy)
        {
            _abiEncoder = abiEncoder;
            _callableContractProxy = callableContractProxy;
        }

        public Address[] GetValidators(Address contractAddress)
        {
            var data = _abiEncoder.Encode(Definition.GetFunction(GetValidatorsFunction).GetCallInfo());

            var returnData = _callableContractProxy.Call(contractAddress, data);

            return DecodeAddresses(returnData);
        }

        private Address[] DecodeAddresses(byte[] data)
        {
            if (data.Length == 0)
            {
                return new Address[] { };
            }
            var objects = _abiEncoder.Decode(Definition.GetFunction(GetValidatorsFunction).GetReturnInfo(), data);
            return GetAddresses(objects);
        }

        private static Address[] GetAddresses(object[] objects)
        {
            return (Address[])objects[0];
        }
    }
}
