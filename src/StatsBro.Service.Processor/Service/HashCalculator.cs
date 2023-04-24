namespace StatsBro.Service.Processor.Service;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StatsBro.Domain.Config;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

public interface IHashCalculator
{
    string Calculate(params object?[] rawArg);
}

public interface IHashCalculatorFactory
{
    IHashCalculator Create();
}

public class HashCalculatorFactory : IHashCalculatorFactory
{
    private readonly ServiceConfig _serviceConfig;
    private readonly ILogger<HashCalculatorFactory> _logger;

    public HashCalculatorFactory(IOptions<ServiceConfig> serviceConfigOptions, ILogger<HashCalculatorFactory> logger)
    {
        this._serviceConfig = serviceConfigOptions.Value;
        this._logger = logger;
    }

    public IHashCalculator Create()
    {
        string pepper;

        if (this._serviceConfig.Pepper != "")
        {
            pepper = this._serviceConfig.Pepper;
        }
        else
        {
            using var store = new X509Store(StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadOnly);
            var certs = store.Certificates.Find(X509FindType.FindBySubjectName, this._serviceConfig.PepperCertSubjectName, false);
            if (certs.Count == 0)
            {
                this._logger.LogError("Certificate is missing, encryption will not be so strong");
                pepper = "111111111111222222222223333333333344444444444455555555555555666666666666666666";
            }
            else
            {
                var certificatePrivateKey = certs[0].GetRSAPrivateKey();
                if (certificatePrivateKey == null)
                {
                    this._logger.LogError("Certificate does not have a private RSA key, encryption will not be so strong");
                    pepper = "111111111111222222222223333333333344444444444455555555555555666666666666666666";
                }
                else
                {
                    // New CERT with keys: https://www.scottbrady91.com/openssl/creating-rsa-keys-using-openssl
                    using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(_serviceConfig.HashKey));
                    var shaHash = SHA512.HashData(certificatePrivateKey.ExportRSAPrivateKey());
                    var privKeyBytes = certificatePrivateKey.ExportRSAPrivateKey();
                    var privKeyString = Convert.ToBase64String(privKeyBytes);
                    var hmacHash = hmac.ComputeHash(certificatePrivateKey.ExportRSAPrivateKey());
                    var justHmac = Convert.ToBase64String(hmacHash);
                    pepper = Convert.ToBase64String(shaHash.Concat(hmacHash).ToArray());
                    this._logger.LogInformation("Will be using cert for hashing");
                }
            }
        }

        return HashCalculator.NewHashCalculator(pepper);
    }
}

public class HashCalculator : IHashCalculator
{
    private string _pepper = "";

    public static IHashCalculator NewHashCalculator(string pepper)
    {
        return new HashCalculator() { _pepper = pepper };
    }

    private HashCalculator() {}

    public string Calculate(params object?[] rawArgs)
    {
        var rawString = string.Join('|', rawArgs);
        rawString = this._pepper + "|" + rawString;
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawString));
        return Convert.ToBase64String(bytes, 0, bytes.Length);
    }
}
