using System;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace Catalyst.Node.Common.Exceptions
{
    /// <summary>
    /// </summary>
    [Serializable]
    public class UnsupportedPlatformException : Exception, ISerializable
    {
        /// <summary>
        ///     Initializes new instance of UnsupportedPlatformException class
        /// </summary>
        /// <param name="message"></param>
        public UnsupportedPlatformException(string message) : base(message) { }
        
        /// <summary>
        ///      Protected constructor used for deserialization/ 
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected UnsupportedPlatformException(SerializationInfo info, 
            StreamingContext context ) :
            base( info, context )
        { }
        
        /// <summary>
        ///     GetObjectData performs a custom serialization.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        [SecurityPermissionAttribute(SecurityAction.Demand,SerializationFormatter=true)]
        public override void GetObjectData( SerializationInfo info, 
            StreamingContext context ) 
        {
            base.GetObjectData( info, context );
        }
    }
}