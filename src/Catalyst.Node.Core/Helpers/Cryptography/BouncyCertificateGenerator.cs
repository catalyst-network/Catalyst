using System;
using System.Collections;
using System.Security;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Prng;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;

namespace Catalyst.Node.Core.Helpers.Cryptography
{
    public class BouncyCertificateGenerator
    {
        /// <summary>
            /// 
            /// </summary>
            /// <remarks>Based on <see cref="http://www.fkollmann.de/v2/post/Creating-certificates-using-BouncyCastle.aspx"/></remarks>
            /// <param name="subjectName"></param>
            /// <returns></returns>
            public static byte[] GenerateCertificate(SecureString password, string subjectName = "localhost")
            {
                var kpgen = new RsaKeyPairGenerator();

                kpgen.Init(new KeyGenerationParameters(new SecureRandom(new CryptoApiRandomGenerator()), 1024));

                var kp = kpgen.GenerateKeyPair();

                var gen = new X509V3CertificateGenerator();

                var certName = new X509Name("CN=" + subjectName);
                var serialNo = BigInteger.ProbablePrime(120, new Random());

                gen.SetSerialNumber(serialNo);
                gen.SetSubjectDN(certName);
                gen.SetIssuerDN(certName);
                gen.SetNotAfter(DateTime.Now.AddYears(100));
                gen.SetNotBefore(DateTime.Now.Subtract(new TimeSpan(7, 0, 0, 0)));
                gen.SetSignatureAlgorithm("MD5WithRSA");
                gen.SetPublicKey(kp.Public);

                gen.AddExtension(
                    X509Extensions.AuthorityKeyIdentifier.Id,
                    false,
                    new AuthorityKeyIdentifier(
                        SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(kp.Public),
                        new GeneralNames(new GeneralName(certName)),
                        serialNo));

                gen.AddExtension(
                    X509Extensions.ExtendedKeyUsage.Id,
                    false,
                    new ExtendedKeyUsage(new ArrayList() { new DerObjectIdentifier("1.3.6.1.5.5.7.3.1") }));

                var newCert = gen.Generate(kp.Private);
                var rawCert = DotNetUtilities.ToX509Certificate(newCert).Export(System.Security.Cryptography.X509Certificates.X509ContentType.Pkcs12, "password");
                
                return rawCert;
            }
        }
    
}