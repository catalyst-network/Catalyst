using AutoMapper;
using Catalyst.Core.Lib.DAO.Converters;
using Catalyst.Protocol.Transaction;
using Nethermind.Dirichlet.Numerics;

namespace Catalyst.Core.Lib.DAO
{
    public class BaseEntryDao : DaoBase<BaseEntry, BaseEntryDao>
    {
        public string ReceiverPublicKey { get; set; }
        public string SenderPublicKey { get; set; }
        public UInt256 TransactionFees { get; set; }

        public override void InitMappers(IMapperConfigurationExpression cfg)
        {
            cfg.CreateMap<BaseEntry, BaseEntryDao>()
               .ForMember(d => d.ReceiverPublicKey, opt => opt.ConvertUsing(new ByteStringToStringPubKeyConverter(), s => s.ReceiverPublicKey))
               .ForMember(d => d.SenderPublicKey, opt => opt.ConvertUsing(new ByteStringToStringPubKeyConverter(), s => s.SenderPublicKey))
               .ForMember(d => d.TransactionFees, opt => opt.ConvertUsing(new ByteStringToUInt256Converter(), s => s.TransactionFees));

            cfg.CreateMap<BaseEntryDao, BaseEntry>()
               .ForMember(d => d.ReceiverPublicKey, opt => opt.ConvertUsing(new StringKeyUtilsToByteStringFormatter(), s => s.ReceiverPublicKey))
               .ForMember(d => d.SenderPublicKey, opt => opt.ConvertUsing(new StringKeyUtilsToByteStringFormatter(), s => s.SenderPublicKey))
               .ForMember(d => d.TransactionFees, opt => opt.ConvertUsing(new UInt256ToByteStringConverter(), s => s.TransactionFees));

        }
    }
}
