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

//using System;
//using Catalyst.Abstractions.Cryptography;
//using Catalyst.Abstractions.Validators;
//using Catalyst.Core.Lib.Validators;
//using Catalyst.Core.Modules.Cryptography.BulletProofs;
//using Catalyst.Protocol.Network;
//using Catalyst.Protocol.Wire;
//using Catalyst.Protocol.Transaction;
//using Google.Protobuf;
//using Google.Protobuf.WellKnownTypes;
//using NSubstitute;
//using Serilog;
//using NUnit.Framework;

//namespace Catalyst.Protocol.Tests.Validators
//{
//    public class TransactionBroadcastTests
//    {
//        private readonly IWrapper _cryptoWrapper;
//        private readonly NetworkType _NetworkType = NetworkType.Devnet;
//        private TransactionBroadcast _transactionBroadcast;
//        private IPrivateKey _privateKey;

//        public TransactionBroadcastTests()
//        {
//            var logger = Substitute.For<ILogger>();
//            _cryptoWrapper = new CryptoWrapper();
//            _transactionValidator = new TransactionValidator(logger, _cryptoWrapper);
//        }

//        [Test]
//        public void Can_Allow_Zero_Amount_Transaction_When_Calling_Smart_Contract_Method()
//        {
//            GenerateTransaction(TransactionType.Normal);
//            _transactionBroadcast.PublicEntries[0].Amount = 0;
//            _transactionBroadcast.Data = ByteString.CopyFromUtf8("MyCallMethod()");
//            GenerateSignature();

//            AssertTransaction(true);
//        }

//        [Test]
//        public void Can_Reject_Contract_Transaction_If_Deployment_Data_And_Call_Data_Exist()
//        {
//            GenerateTransaction(TransactionType.Normal);
//            _transactionBroadcast.Init = ByteString.CopyFromUtf8("ASmartContractDeployment");
//            _transactionBroadcast.Data = ByteString.CopyFromUtf8("SmartContractCallData(1,1)");
//            GenerateSignature();

//            AssertTransaction(false);
//        }

//        [Test]
//        public void Can_Reject_ST_Entries_Inside_Conf_Transaction()
//        {
//            GenerateTransaction(TransactionType.Confidential);
//            _transactionBroadcast.PublicEntries.Add(new PublicEntry());
//            GenerateSignature();
//            AssertTransaction(false);
//        }

//        [Test]
//        public void Can_Reject_CF_Entries_Inside_Public_Transaction()
//        {
//            GenerateTransaction(TransactionType.Normal);
//            _transactionBroadcast.CFEntries.Add(new ConfidentialEntry());
//            GenerateSignature();
//            AssertTransaction(false);
//        }

//        [Test]
//        public void Can_Reject_No_Signature()
//        {
//            GenerateTransaction(TransactionType.Normal);
//            _transactionBroadcast.Signature = ByteString.Empty;
//            AssertTransaction(false);
//        }

//        [Test]
//        public void Can_Reject_Invalid_From_Public_Key()
//        {
//            GenerateTransaction(TransactionType.Normal);
//            _transactionBroadcast.From = ByteString.CopyFrom(new byte[22]);
//            GenerateSignature();
//            AssertTransaction(false);
//        }

//        [Test]
//        public void Can_Reject_Invalid_Signature()
//        {
//            GenerateTransaction(TransactionType.Normal);
//            _transactionBroadcast.Signature = ByteString.CopyFrom(new byte[_cryptoWrapper.SignatureLength]);
//            AssertTransaction(false);
//        }

//        [Test]
//        public void Can_Reject_Any_ST_Entries_In_Smart_Contract_Deployment()
//        {
//            GenerateTransaction(TransactionType.Normal);
//            _transactionBroadcast.Init = ByteString.CopyFromUtf8("AFakeSmartContractDeploy");
//            GenerateSignature();
//            AssertTransaction(false);
//        }

//        [Test]
//        public void Can_Reject_Any_CF_Entries_In_Smart_Contract_Deployment()
//        {
//            GenerateTransaction(TransactionType.Confidential);
//            _transactionBroadcast.Init = ByteString.CopyFromUtf8("AFakeSmartContractDeploy");
//            GenerateSignature();
//            AssertTransaction(false);
//        }

//        [Test]
//        public void Can_Reject_Normal_Transactions_With_No_Entries()
//        {
//            GenerateTransaction(TransactionType.Normal);
//            _transactionBroadcast.PublicEntries.Clear();
//            GenerateSignature();
//            AssertTransaction(false);
//        }

//        [Test]
//        public void Can_Reject_Conf_Transactions_With_No_Entries()
//        {
//            GenerateTransaction(TransactionType.Confidential);
//            _transactionBroadcast.CFEntries.Clear();
//            GenerateSignature();
//            AssertTransaction(false);
//        }

//        [Test]
//        public void Can_Reject_Null_Timestamp()
//        {
//            GenerateTransaction(TransactionType.Normal);
//            _transactionBroadcast.TimeStamp = null;
//            GenerateSignature();
//            AssertTransaction(false);
//        }

//        [Test]
//        public void Can_Reject_Invalid_Public_Key_On_ST_Entries()
//        {
//            GenerateTransaction(TransactionType.Normal);
//            _transactionBroadcast.PublicEntries[0].PubKey = ByteString.CopyFromUtf8("SomeBadPublicKey");
//            GenerateSignature();
//            AssertTransaction(false);
//        }

//        [Test]
//        public void Can_Reject_Invalid_Amount_On_ST_Entries()
//        {
//            GenerateTransaction(TransactionType.Normal);
//            _transactionBroadcast.PublicEntries[0].Amount = 0;
//            GenerateSignature();
//            AssertTransaction(false);
//        }

//        [Test]
//        public void Can_Reject_Invalid_Pedersen_Commit_On_CF_Entries()
//        {
//            GenerateTransaction(TransactionType.Confidential);
//            _transactionBroadcast.CFEntries[0].PedersenCommitment = ByteString.Empty;
//            GenerateSignature();

//            AssertTransaction(false);
//        }

//        [Test]
//        public void Can_Reject_Invalid_Public_Key_On_CF_Entries()
//        {
//            GenerateTransaction(TransactionType.Confidential);
//            _transactionBroadcast.CFEntries[0].PubKey = ByteString.CopyFromUtf8("SomeBadPublicKey");
//            GenerateSignature();

//            AssertTransaction(false);
//        }

//        [Test]
//        public void Can_Pass_Successful_Normal_Transaction()
//        {
//            GenerateTransaction(TransactionType.Normal);
//            AssertTransaction(true);
//        }

//        [Test]
//        public void Can_Pass_Successful_Conf_Transaction()
//        {
//            GenerateTransaction(TransactionType.Confidential);
//            AssertTransaction(true);
//        }

//        public void AssertTransaction(bool valid)
//        {
//            _transactionValidator.ValidateTransaction(_transactionBroadcast, _network).Should().Be(valid);
//        }

//        public void GenerateTransaction(TransactionType type)
//        {
//            _privateKey = _cryptoWrapper.GeneratePrivateKey();
//            var publicKey = ByteString.CopyFrom(_privateKey.GetPublicKey().Bytes);
//            _transactionBroadcast = new TransactionBroadcast
//            {
//                LockTime = 0,
//                TimeStamp = Timestamp.FromDateTime(DateTime.UtcNow),
//                TransactionType = type,
//                Signature = ByteString.Empty,
//                Data = ByteString.Empty,
//                Init = ByteString.Empty,
//                From = publicKey
//            };

//            if (type == TransactionType.Normal)
//            {
//                _transactionBroadcast.PublicEntries.Add(new PublicEntry
//                {
//                    Amount = 10,
//                    PubKey = publicKey
//                });
//            }
//            else
//            {
//                _transactionBroadcast.CFEntries.Add(new ConfidentialEntry
//                {
//                    PedersenCommitment = ByteString.CopyFromUtf8("FakeCommit"),
//                    PubKey = publicKey
//                });
//            }

//            GenerateSignature();
//        }

//        public void GenerateSignature()
//        {
//            var transactionWithoutSig = _transactionBroadcast.Clone();
//            transactionWithoutSig.Signature = ByteString.Empty;

//            byte[] signatureBytes = _cryptoWrapper.StdSign(_privateKey, transactionWithoutSig.ToByteArray(),
//                new SigningContext
//                {
//                    NetworkType = _network,
//                    SignatureType = transactionWithoutSig.TransactionType == TransactionType.Normal
//                        ? SignatureType.TransactionPublic
//                        : SignatureType.TransactionConfidential
//                }.ToByteArray()).SignatureBytes;
//            _transactionBroadcast.Signature = ByteString.CopyFrom(signatureBytes);
//        }
//    }
//}
