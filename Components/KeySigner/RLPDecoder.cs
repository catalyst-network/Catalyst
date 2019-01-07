using System.Collections.Generic;
using ADL.RLP;

namespace ADL.KeySigner
{
    public class RLPDecoder
    {
        public static SignedData DecodeSigned(byte[] rawdata, int numberOfEncodingElements)
        {
            var decodedList = RLP.RLP.Decode(rawdata);
            var decodedData = new List<byte[]>();
            var decodedElements = (RLPCollection)decodedList[0];
            AtlasECDSASignature signature = null;
            for (var i = 0; i < numberOfEncodingElements; i++)
                decodedData.Add(decodedElements[i].RLPData);
            // only parse signature in case is signed
            if (decodedElements[numberOfEncodingElements].RLPData != null)
            {
                //Decode Signature
                var v = decodedElements[numberOfEncodingElements].RLPData;
                var r = decodedElements[numberOfEncodingElements + 1].RLPData;
                var s = decodedElements[numberOfEncodingElements + 2].RLPData;
                signature = AtlasECDSASignatureFactory.FromComponents(r, s, v);
            }
            return new SignedData(decodedData.ToArray(), signature);
        }
    }
}