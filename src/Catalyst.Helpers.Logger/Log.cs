using System;

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
    }
}