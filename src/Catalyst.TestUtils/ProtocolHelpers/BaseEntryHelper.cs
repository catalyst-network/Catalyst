//#region LICENSE

///**
//* Copyright (c) 2019 Catalyst Network
//*
//* This file is part of Catalyst.Node <https://github.com/catalyst-network/Catalyst.Node>
//*
//* Catalyst.Node is free software: you can redistribute it and/or modify
//* it under the terms of the GNU General Public License as published by
//* the Free Software Foundation, either version 2 of the License, or
//* (at your option) any later version.
//*
//* Catalyst.Node is distributed in the hope that it will be useful,
//* but WITHOUT ANY WARRANTY; without even the implied warranty of
//* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//* GNU General Public License for more details.
//*
//* You should have received a copy of the GNU General Public License
//* along with Catalyst.Node. If not, see <https://www.gnu.org/licenses/>.
//*/

//#endregion

//using System;
//using Catalyst.Core.Lib.DAO;
//using Catalyst.Core.Lib.DAO.Transaction;
//using Catalyst.Core.Lib.Extensions;
//using Catalyst.Core.Lib.Util;
//using Catalyst.Protocol.Transaction;
//using Nethermind.Core.Extensions;

//namespace Catalyst.TestUtils.ProtocolHelpers
//{
//    public static class BaseEntryHelper
//    {
//        private static readonly IMapperProvider MapperProvider = new TestMapperProvider();

//        public static BaseEntry GetBaseEntry()
//        {
//            var fees = new Random().Next(78588446).ToByteArray(new Bytes.Endianness());

//            return new BaseEntry
//            {
//                TransactionFees = fees.ToByteString(),
//                ReceiverPublicKey = ByteUtil.GenerateRandomByteArray(32).ToByteString(),
//                SenderPublicKey = ByteUtil.GenerateRandomByteArray(32).ToByteString(),
//            };
//        }

//        public static BaseEntryDao GetBaseEntryDao()
//        {
//            return GetBaseEntry().ToDao<BaseEntry, BaseEntryDao>(MapperProvider);
//        }
//    }
//}
