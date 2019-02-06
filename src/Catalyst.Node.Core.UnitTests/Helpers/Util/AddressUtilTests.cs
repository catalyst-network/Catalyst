using Catalyst.Node.Core.Helpers.Util;
using FluentAssertions;
using Xunit;

namespace Catalyst.Node.UnitTests.Helpers.Util
{
    public class AddressUtilTests
    {
        public string ToChecksumAddress(string address)
        {
            return new AddressUtil().ConvertToChecksumAddress(address);
        }

        [Fact]
        public virtual void ShouldCheckIsCheckSumAddress()
        {
            var address1 = "0x5aAeb6053F3E94C9b9A09f33669435E7Ef1BeAed";
            var address1F = "0x5aaeb6053F3E94C9b9A09f33669435E7Ef1BeAed";
            var address2 = "0xfB6916095ca1df60bB79Ce92cE3Ea74c37c5d359";
            var address2F = "0xfb6916095ca1df60bB79Ce92cE3Ea74c37c5d359";
            var address3 = "0xdbF03B407c01E7cD3CBea99509d93f8DDDC8C6FB";
            var address4 = "0xD1220A0cf47c7B9Be7A2E6BA89F429762e7b9aDb";
            var addressUtil = new AddressUtil();
            addressUtil.IsChecksumAddress(address1).Should().BeTrue();
            addressUtil.IsChecksumAddress(address1F).Should().BeFalse();
            addressUtil.IsChecksumAddress(address2).Should().BeTrue();
            addressUtil.IsChecksumAddress(address2F).Should().BeFalse();
            addressUtil.IsChecksumAddress(address3).Should().BeTrue();
            addressUtil.IsChecksumAddress(address4).Should().BeTrue();
        }

        [Fact]
        public virtual void ShouldCreateACheckSumAddress()
        {
            var address1 = "0x5aAeb6053F3E94C9b9A09f33669435E7Ef1BeAed";
            var address2 = "0xfB6916095ca1df60bB79Ce92cE3Ea74c37c5d359";
            var address3 = "0xdbF03B407c01E7cD3CBea99509d93f8DDDC8C6FB";
            var address4 = "0xD1220A0cf47c7B9Be7A2E6BA89F429762e7b9aDb";
            ToChecksumAddress(address1.ToUpper()).Should().Be(address1);
            ToChecksumAddress(address2.ToUpper()).Should().Be(address2);
            ToChecksumAddress(address3.ToUpper()).Should().Be(address3);
            ToChecksumAddress(address4.ToUpper()).Should().Be(address4);
        }

        [Fact]
        public virtual void ShouldValidateAddressHexFormat()
        {
            var address1 = "0x5aAeb6053F3E94C9b9A09f33669435E7Ef1BeAed";
            var address2 = "0x5aaeb6053F3E94C9b9A09f33669435E7Ef1BeAed";
            address1.IsValidCatalystAddressHexFormat().Should().BeTrue();
            address2.IsValidCatalystAddressHexFormat().Should().BeTrue();

            var address3 = "5aAeb6053F3E94C9b9A09f33669435E7Ef1BeAed";
            address3.IsValidCatalystAddressHexFormat().Should().BeFalse();
            //length
            var address4 = "0x5aAeb6053F3E94C9b9A09f33669435E7Ef1Be";
            address4.IsValidCatalystAddressHexFormat().Should().BeFalse();
            //non alpha
            //length
            var address5 = "0x5aAeb6053F3E94C9b9A09f33669435E7Ef1BeA'#";
            address5.IsValidCatalystAddressHexFormat().Should().BeFalse();
        }
    }
}