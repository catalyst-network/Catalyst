using Catalyst.Protocol.Transaction;
using Catalyst.Protocol.Wire;
using Google.Protobuf;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Hex.HexTypes;
using Nethereum.Web3;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Catalyst.Cli.SmartContractConsole
{
    [FunctionOutput]
    public class Output
    {

        [Parameter("address[]", "", 1)]
        public List<string> address { get; set; }
    }

    class Program
    {
        static async Task Main(string[] args)
        {
            var abi = File.ReadAllText("KovanAbi.txt");
            var web3 = new Web3("http://127.0.0.1:5005/api/eth/request");

            var contract = web3.Eth.GetContract(abi, "0x2932E7Af4fB3936c62eEff34Bf9c4448f1C2E63c");
            var function = contract.GetFunction("getValidators");
            var data = function.DecodeDTOTypeOutput<Output>("0x00000000000000000000000000000000000000000000000000000000000000200000000000000000000000000000000000000000000000000000000000000001000000000000000000000000b77aec9f59f9d6f39793289a09aea871932619ed");
            //var data = function.DecodeInput("0x00000000000000000000000000000000000000000000000000000000000000200000000000000000000000000000000000000000000000000000000000000001000000000000000000000000b77aec9f59f9d6f39793289a09aea871932619ed");


            //var res = await function.SendTransactionAsync("0x58beb247771f0b6f87aa099af479af767ccc0f00");
            //function.
            //define the address
            //string address = "0x58beb247771f0b6f87aa099af479af767ccc0f00";

            //Create an array of objects for the param
            //object[] param = new object[1] { address };

            //Call the function and pass in the params 
            //var result = await function.CallAsync<byte[]>();

            var a = 0;

            //var transactionBroadcast = new TransactionBroadcast();
            //var publicEntry = new PublicEntry();
            //publicEntry.Amount = ByteString.CopyFrom(BitConverter.GetBytes(1));
            //publicEntry.GasLimit = 100_000;
            //publicEntry.GasPrice = ByteString.CopyFrom(BitConverter.GetBytes(5));
            //public
            //transactionBroadcast.PublicEntry = publicEntry;

            //var from = "0xb60e8dd61c5d32be8058bb8eb970870f07233155";
            //var to = "0xd46e8dd67c5d32be8058bb8eb970870f07244567";
            //var gas = "0x76c0";
            //var gasPrice = "0x9184e72a000";
            //var value = "0x9184e72a";
            //var data = "0xd46e8dd67c5d32be8d46e8dd67c5d32be8058bb8eb970870f072445675058bb8eb970870f072445675";
            //var call = { "jsonrpc":"2.0","method":"eth_call","params": [{ "from": ,"to": ,"gas": ,"gasPrice": ,"value": ,"data": }, "latest"],"id":1}

            Console.WriteLine("Hello World!");
        }
    }
}
