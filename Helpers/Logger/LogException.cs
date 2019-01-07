using System;

namespace ADL.Log
{
    public class LogException
    {   
        /// <summary>
        /// 
        /// </summary>
        /// <param name="method"></param>
        /// <param name="e"></param>
        public static void Message(string method, Exception e)
        {
            Log.Message("================================================================================");
            Log.Message(" = Method: " + method);
            Log.Message(" = Exception Type: " + e.GetType().ToString());
            Log.Message(" = Exception Data: " + e.Data);
            Log.Message(" = Inner Exception: " + e.InnerException);
            Log.Message(" = Exception Message: " + e.Message);
            Log.Message(" = Exception Source: " + e.Source);
            Log.Message(" = Exception StackTrace: " + e.StackTrace);
            Log.Message("================================================================================");
        }
        
    }
}