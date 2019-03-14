namespace Catalyst.Cli
{
using System.Net;

    public class ClientSettings
    {
        public static bool IsSsl
        {
            get
            {
                //string ssl = Helper.Configuration["ssl"];
                //return !string.IsNullOrEmpty(ssl) && bool.Parse(ssl);
                return false;
            }
        }

        public static IPAddress Host => IPAddress.Parse("127.0.0.1");

        public static int Port => int.Parse("8807");

        public static int Size => int.Parse("256");

        public static bool UseLibuv
        {
            get
            {
                string libuv = Helper.Configuration["libuv"];
                return !string.IsNullOrEmpty(libuv) && bool.Parse(libuv);
            }
        }
    }
}