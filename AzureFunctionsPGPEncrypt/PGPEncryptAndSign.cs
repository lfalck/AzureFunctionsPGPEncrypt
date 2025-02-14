using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text;
using PgpCore;
using Org.BouncyCastle.Bcpg.OpenPgp;
using Microsoft.Extensions.Configuration;

namespace AzureFunctionsPGPEncrypt;

public class PGPEncryptAndSign
{
    private readonly ILogger<PGPEncryptAndSign> _logger;
    private readonly IConfiguration _configuration;

    public PGPEncryptAndSign(ILogger<PGPEncryptAndSign> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    [Function(nameof(PGPEncryptAndSign))]
    public async Task<IActionResult> RunAsync([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
    {
        _logger.LogInformation($"C# HTTP trigger function {nameof(PGPEncryptAndSign)} processed a request.");

        string publicKeyBase64 = _configuration["pgp-public-key"];
        string privateKeySignBase64 = _configuration["pgp-private-key-sign"];
        string passPhraseSign = _configuration["pgp-passphrase-sign"];

        if (string.IsNullOrEmpty(publicKeyBase64))
        {
            return new BadRequestObjectResult($"Please add a base64 encoded public key to an environment variable called pgp-public-key");
        }

        if (string.IsNullOrEmpty(privateKeySignBase64))
        {
            return new BadRequestObjectResult($"Please add a base64 encoded private key to an environment variable called pgp-private-key-sign");
        }

        byte[] publicKeyBytes = Convert.FromBase64String(publicKeyBase64);
        string publicKey = Encoding.UTF8.GetString(publicKeyBytes);

        byte[] privateKeySignBytes = Convert.FromBase64String(privateKeySignBase64);
        string privateKeySign = Encoding.UTF8.GetString(privateKeySignBytes);

        req.EnableBuffering(); //Make RequestBody Stream seekable

        try
        {
            Stream encryptedData = await EncryptAndSignAsync(req.Body, publicKey, privateKeySign, passPhraseSign);
            return new OkObjectResult(encryptedData);
        }
        catch (PgpException pgpException)
        {
            return new BadRequestObjectResult(pgpException.Message);
        }
    }

    private static async Task<Stream> EncryptAndSignAsync(Stream inputStream, string publicKey, string privateKey, string passPhrase)
    {
        using (PGP pgp = new PGP(new EncryptionKeys(publicKey, privateKey, passPhrase)))
        {
            var outputStream = new MemoryStream();

            using (inputStream)
            {
                await pgp.EncryptStreamAndSignAsync(inputStream, outputStream, true, true);
                outputStream.Seek(0, SeekOrigin.Begin);
                return outputStream;
            }
        }
    }
}
