#region LICENSE

/**
* Copyright (c) 2019 Catalyst Network
*
* This file is part of Catalyst.Node <https://github.com/catalyst-network/Catalyst.Node>
*
* Catalyst.Node is free software: you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation, either version 2 of the License, or
* (at your option) any later version.
*
* Catalyst.Node is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
* GNU General Public License for more details.
*
* You should have received a copy of the GNU General Public License
* along with Catalyst.Node. If not, see <https://www.gnu.org/licenses/>.
*/

#endregion

using AutoMapper;
using Google.Protobuf;

namespace Catalyst.Core.Lib.DAO.Converters
{
    public class ByteStringToValueCommitmentBase64Converter : IValueConverter<ByteString, ValueCommitmentDao>
    {
        public ValueCommitmentDao Convert(ByteString sourceMember, ResolutionContext context)
        {
            var store = sourceMember.ToBase64();

            return new ValueCommitmentDao
            {
                Value = store
            };
        }
    }

    public class ValueCommitmentDaoToByteStringConverter : IValueConverter<ValueCommitmentDao, ByteString>
    {
        public ByteString Convert(ValueCommitmentDao sourceMember, ResolutionContext context)
        {
            return ByteString.FromBase64(sourceMember.Value);
        }
    }
    
    public class ByteStringToAggregatedVectorPolynomialLBase64Converter : IValueConverter<ByteString, AggregatedVectorPolynomialLDao>
    {
        public AggregatedVectorPolynomialLDao Convert(ByteString sourceMember, ResolutionContext context)
        {
            var store = sourceMember.ToBase64();

            return new AggregatedVectorPolynomialLDao
            {
                Value = store
            };
        }
    }

    public class AggregatedVectorPolynomialLDaoToByteStringConverter : IValueConverter<AggregatedVectorPolynomialLDao, ByteString>
    {
        public ByteString Convert(AggregatedVectorPolynomialLDao sourceMember, ResolutionContext context)
        {
            return ByteString.FromBase64(sourceMember.Value);
        }
    }

    public class ByteStringToAggregatedVectorPolynomialRBase64Converter : IValueConverter<ByteString, AggregatedVectorPolynomialRDao>
    {
        public AggregatedVectorPolynomialRDao Convert(ByteString sourceMember, ResolutionContext context)
        {
            var store = sourceMember.ToBase64();

            return new AggregatedVectorPolynomialRDao
            {
                Value = store
            };
        }
    }

    public class AggregatedVectorPolynomialRDaoToByteStringConverter : IValueConverter<AggregatedVectorPolynomialRDao, ByteString>
    {
        public ByteString Convert(AggregatedVectorPolynomialRDao sourceMember, ResolutionContext context)
        {
            return ByteString.FromBase64(sourceMember.Value);
        }
    }

    public class ByteStringToStringBase64Converter : IValueConverter<ByteString, string>
    {
        public string Convert(ByteString sourceMember, ResolutionContext context)
        {
            return sourceMember.ToBase64();
        }
    }

    public class StringBase64ToByteStringConverter : IValueConverter<string, ByteString>
    {
        public ByteString Convert(string sourceMember, ResolutionContext context)
        {
            return ByteString.FromBase64(sourceMember);
        }
    }
}
