using System.Threading.Tasks;
using Mono.Nat;
using NSubstitute;

namespace Catalyst.UPnP.Tests.TestUtils
{
    public static class TestUtils
    {
        public static INatDevice GetTestDeviceWithExistingMappings(Mapping[] existingMappings)
        {
            var device = Substitute.For<INatDevice>();
            device.GetAllMappingsAsync().Returns(existingMappings);
            device.CreatePortMapAsync(Arg.Any<Mapping>()).ReturnsForAnyArgs(x => x.Arg<Mapping>());
            device.DeletePortMapAsync(Arg.Any<Mapping>()).ReturnsForAnyArgs(x => x.Arg<Mapping>());
            return device;
        }
    }
}
