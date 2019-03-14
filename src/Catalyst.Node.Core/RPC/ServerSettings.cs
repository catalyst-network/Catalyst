namespace Catalyst.Node.Core.RPC
{
    public static class ServerSettings
    {
        public static bool IsSsl
        {
            get
            {
                //string ssl = Helper.Configuration["ssl"];
                //return !string.IsNullOrEmpty(ssl) && bool.Parse(ssl);
                return true;
            }
        }

        public static int Port => int.Parse("30303");

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