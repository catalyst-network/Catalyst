using System;
using System.Collections;

namespace Catalyst.Helpers.Logger
{
    public static class Log
    {
        /// <summary>
        /// </summary>
        /// <param name="msg"></param>
        public static void Message(string msg)
        {
            Console.WriteLine(msg);
        }

        /// <summary>
        /// </summary>
        /// <param name="byteArrMsg"></param>
        public static void ByteArr(byte[] byteArrMsg)
        {
            Console.WriteLine(BitConverter.ToString(byteArrMsg));
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="col"></param>
        /// <typeparam name="T"></typeparam>
        public static void PrintCollection<T>(IEnumerable col)
        {
            foreach(var item in col)
                Console.WriteLine(item); // Replace this with your version of printing
        }
    }
}