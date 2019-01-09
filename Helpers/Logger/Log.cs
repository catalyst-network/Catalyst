using System;

namespace ADL.Log
{
    public static class Log
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="msg"></param>
        public static void Message(string msg)
        {
            Console.WriteLine(msg);
        }
        
        public static void ByteArr(byte[] byteArrMsg)
        {
            Console.WriteLine(BitConverter.ToString(byteArrMsg));
        }
    }
}