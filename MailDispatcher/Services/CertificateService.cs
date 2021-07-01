using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace MailDispatcher.Services
{
    [DIRegister(ServiceLifetime.Singleton)]
    public class CertificateService
    {
        public CertificateService()
        {

        }

        public X509Certificate2 BuildSelfSignedServerCertificate(string crtificateName = "NSSelfSignedCert")
        {
            SubjectAlternativeNameBuilder sanBuilder = new SubjectAlternativeNameBuilder();
            // sanBuilder.AddIpAddress(IPAddress.Loopback);
            // sanBuilder.AddIpAddress(IPAddress.IPv6Loopback);
            sanBuilder.AddDnsName("localhost");
            sanBuilder.AddDnsName(Environment.MachineName);

            X500DistinguishedName distinguishedName = new X500DistinguishedName($"CN={crtificateName}");

            using (RSA rsa = RSA.Create(2048))
            {
                var request = new CertificateRequest(distinguishedName, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

                request.CertificateExtensions.Add(
                    new X509KeyUsageExtension(X509KeyUsageFlags.DataEncipherment | X509KeyUsageFlags.KeyEncipherment | X509KeyUsageFlags.DigitalSignature, false));


                request.CertificateExtensions.Add(
                   new X509EnhancedKeyUsageExtension(
                       new OidCollection { new Oid("1.3.6.1.5.5.7.3.1") }, false));

                request.CertificateExtensions.Add(sanBuilder.Build());

                var certificate = request.CreateSelfSigned(new DateTimeOffset(DateTime.UtcNow.AddDays(-1)), new DateTimeOffset(DateTime.UtcNow.AddDays(3650)));
                certificate.FriendlyName = crtificateName;

                return new X509Certificate2(certificate.Export(X509ContentType.Pfx, "WeNeedASaf3rPassword"), "WeNeedASaf3rPassword", X509KeyStorageFlags.MachineKeySet);
            }
        }

        public X509Certificate GetCertificate(string domain)
        {
            var now = DateTime.UtcNow;
            using (X509Store store = new X509Store("WebHosting", StoreLocation.LocalMachine))
            {
                store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);
                foreach (var existing in store.Certificates)
                {
                    if (existing.NotAfter <= now) continue;
                    if (existing.PrivateKey == null) continue;
                    var names = GetNames(existing);
                    if (names.Any(x => x.Equals($"*.{domain}")))
                    {
                        return existing;
                    }
                }
            }
            throw new KeyNotFoundException();

        }

        IEnumerable<string> GetNames(X509Certificate2 certificate)
        {
            System.Security.Cryptography.X509Certificates.X509Extension uccSan = certificate.Extensions["2.5.29.17"];
            if (uccSan != null)
            {
                foreach (string nvp in uccSan.Format(true).Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
                {
                    string[] parts = nvp.Split('=');
                    string name = parts[0];
                    string value = (parts.Length > 0) ? parts[1] : null;
                    yield return value;
                }
            }
        }
    }
}
