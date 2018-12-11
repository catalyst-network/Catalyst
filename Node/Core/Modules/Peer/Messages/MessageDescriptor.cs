namespace ADL.Node.Core.Modules.Peer.Messages
{
    public class MessageDescriptor
    {
        /// <summary>
        /// Builds a p2p message header
        /// @TODO we need to make sure header byte length is always same, do we want to check version is 4 bytes long now or when we load settings?
        /// </summary>
        /// <param name="network"></param>
        /// <param name="version"></param>
        /// <param name="type"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static byte[] BuildDiscriptor(byte network, byte version, byte type)
        {
            return new []{network, version, type};
        }
    }
}