using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Bcpg.OpenPgp;
using PgpCore;
using System.Text;

namespace AzureFunctionsPGPEncrypt;

public class PGPDecrypt
{
    private readonly ILogger<PGPDecrypt> _logger;
    private readonly IConfiguration _configuration;

    public PGPDecrypt(ILogger<PGPDecrypt> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    [Function(nameof(PGPDecrypt))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
    {
        _logger.LogInformation($"C# HTTP trigger function {nameof(PGPDecrypt)} processed a request.");

        string privateKeyBase64 = _configuration["pgp-private-key"];
        string passPhrase = _configuration["pgp-passphrase"];

        if (string.IsNullOrEmpty(privateKeyBase64))
        {
            return new BadRequestObjectResult($"Please add a base64 encoded private key to an environment variable called pgp-private-key");
        }

        byte[] privateKeyBytes = Convert.FromBase64String(privateKeyBase64);
        string privateKey = Encoding.UTF8.GetString(privateKeyBytes);

        var inputStream = new MemoryStream();
        await req.Body.CopyToAsync(inputStream);
        inputStream.Seek(0, SeekOrigin.Begin);

        try
        {
            Stream decryptedData = await DecryptAsync(inputStream, privateKey, passPhrase);
            return new OkObjectResult(decryptedData);
        }
        catch (PgpException pgpException)
        {
            return new BadRequestObjectResult(pgpException.Message);
        }
    }

    private async Task<Stream> DecryptAsync(Stream inputStream, string privateKey, string passPhrase)
    {
        using (PGP pgp = new PGP(new EncryptionKeys(privateKey, passPhrase)))
        {
            var outputStream = new MemoryStream();

            using (inputStream)
            {
                await pgp.DecryptStreamAsync(inputStream, outputStream);
                outputStream.Seek(0, SeekOrigin.Begin);
                return outputStream;
            }
        }
    }
}