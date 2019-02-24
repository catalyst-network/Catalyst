using System;
using System.Runtime.Serialization;

namespace Catalyst.Node.Common.Exceptions
{
    /// <summary>
    /// </summary>
    [Serializable]
    public class UnsupportedPlatformException : Exception, ISerializable
    {
        public UnsupportedPlatformException(string pfxfileName)
            : base(String.Format("Catalyst network currently doesn't support on the fly creation of self signed certificate. Please create a password protected certificate at {0}." +
                Environment.NewLine +
                "cf. `https://github.com/catalyst-network/Catalyst.Node/wiki/Creating-a-Self-Signed-Certificate` for instructions", pfxfileName))
        {
        }
    }
}