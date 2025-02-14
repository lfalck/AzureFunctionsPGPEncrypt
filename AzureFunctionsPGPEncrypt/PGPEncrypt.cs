using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text;
using PgpCore;
using Org.BouncyCastle.Bcpg.OpenPgp;
using Microsoft.Extensions.Configuration;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace AzureFunctionsPGPEncrypt;

public class PGPEncrypt
{
    private readonly ILogger<PGPEncrypt> _logger;
    private readonly IConfiguration _configuration;

    public PGPEncrypt(ILogger<PGPEncrypt> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    [Function(nameof(PGPEncrypt))]
    public async Task<IActionResult> RunAsync([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
    {
        _logger.LogInformation($"C# HTTP trigger function {nameof(PGPEncrypt)} processed a request.");

        string publicKeyBase64 = _configuration["pgp-public-key"];

        if (string.IsNullOrEmpty(publicKeyBase64))
        {
            return new BadRequestObjectResult($"Please add a base64 encoded public key to an environment variable called pgp-public-key");
        }

        byte[] publicKeyBytes = Convert.FromBase64String(publicKeyBase64);
        string publicKey = Encoding.UTF8.GetString(publicKeyBytes);

        req.EnableBuffering(); //Make RequestBody Stream seekable

        try
        {
            Stream encryptedData = await EncryptAsync(req.Body, publicKey);
            return new OkObjectResult(encryptedData);
        }
        catch (PgpException pgpException)
        {
            return new BadRequestObjectResult(pgpException.Message);
        }
    }

    private static async Task<Stream> EncryptAsync(Stream inputStream, string publicKey)
    {
        using (PGP pgp = new PGP(new EncryptionKeys(publicKey))) 
        {
            var outputStream = new MemoryStream();

            using (inputStream)
            {
                await pgp.EncryptStreamAsync(inputStream, outputStream, true, true);
                outputStream.Seek(0, SeekOrigin.Begin);
                return outputStream;
            }
        }
    }
}
