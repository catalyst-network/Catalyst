using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using ProtoBuf;

namespace MultiFormats
{
    /// <summary>
    ///     Helper methods for ProtoBuf.
    /// </summary>
    public static class ProtoBufHelper
    {
        static MethodInfo writeRawBytes = typeof(CodedOutputStream)
           .GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
           .Single(m =>
                m.Name == "WriteRawBytes" && m.GetParameters().Count() == 1
            );
      
        static MethodInfo readRawBytes = typeof(CodedInputStream)
           .GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
           .Single(m =>
                m.Name == "ReadRawBytes"
            );

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="bytes"></param>
        public static void WriteSomeBytes(this CodedOutputStream stream, byte[] bytes)
        {
            writeRawBytes.Invoke(stream, new object[]
            {
                bytes
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static byte[] ReadSomeBytes(this CodedInputStream stream, int length)
        {
            return (byte[]) readRawBytes.Invoke(stream, new object[]
            {
                length
            });
        }
        
        /// <summary>
        ///     Read a proto buf message with a varint length prefix.
        /// </summary>
        /// <typeparam name="T">
        ///     The type of message.
        /// </typeparam>
        /// <param name="stream">
        ///     The stream containing the message.
        /// </param>
        /// <param name="cancel">
        ///     Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException" /> is raised.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation. The task's result is
        ///     the <typeparamref name="T" /> message.
        /// </returns>
        public static async Task<T> ReadMessageAsync<T>(Stream stream, CancellationToken cancel = default)
        {
            var length = await stream.ReadVarint32Async(cancel).ConfigureAwait(false);
            var bytes = new byte[length];
            await stream.ReadExactAsync(bytes, 0, length, cancel).ConfigureAwait(false);

            using (var ms = new MemoryStream(bytes, false))
            {
                return Serializer.Deserialize<T>(ms);
            }
        }
    }
}
