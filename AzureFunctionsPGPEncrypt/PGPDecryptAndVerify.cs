using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Bcpg.OpenPgp;
using PgpCore;
using System.Text;

namespace AzureFunctionsPGPEncrypt
{
    public class PGPDecryptAndVerify
    {
        private readonly ILogger<PGPDecryptAndVerify> _logger;
        private readonly IConfiguration _configuration;

        public PGPDecryptAndVerify(ILogger<PGPDecryptAndVerify> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        [Function(nameof(PGPDecryptAndVerify))]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        {
            _logger.LogInformation($"C# HTTP trigger function {nameof(PGPDecryptAndVerify)} processed a request.");

            string privateKeyBase64 = _configuration["pgp-private-key"];
            string passPhrase = _configuration["pgp-passphrase"];
            string publicKeyVerifyBase64 = _configuration["pgp-public-key-verify"];

            if (string.IsNullOrEmpty(privateKeyBase64))
            {
                return new BadRequestObjectResult($"Please add a base64 encoded private key to an environment variable called pgp-private-key");
            }

            if (string.IsNullOrEmpty(publicKeyVerifyBase64))
            {
                return new BadRequestObjectResult($"Please add a base64 encoded public key to an environment variable called pgp-public-key-verify");
            }

            byte[] privateKeyBytes = Convert.FromBase64String(privateKeyBase64);
            string privateKey = Encoding.UTF8.GetString(privateKeyBytes);

            byte[] publicKeyVerifyBytes = Convert.FromBase64String(publicKeyVerifyBase64);
            string publicKeyVerify = Encoding.UTF8.GetString(publicKeyVerifyBytes);

            var inputStream = new MemoryStream();
            await req.Body.CopyToAsync(inputStream);
            inputStream.Seek(0, SeekOrigin.Begin);

            try
            {
                Stream decryptedData = await DecryptAndVerifyAsync(inputStream, privateKey, publicKeyVerify, passPhrase);
                return new OkObjectResult(decryptedData);
            }
            catch (PgpException pgpException)
            {
                return new BadRequestObjectResult(pgpException.Message);
            }
        }

        private async Task<Stream> DecryptAndVerifyAsync(Stream inputStream, string privateKey, string publicKeyVerify, string passPhrase)
        {
            using (PGP pgp = new PGP(new EncryptionKeys(publicKeyVerify, privateKey, passPhrase)))
            {
                var outputStream = new MemoryStream();

                using (inputStream)
                {
                    await pgp.DecryptStreamAndVerifyAsync(inputStream, outputStream);
                    outputStream.Seek(0, SeekOrigin.Begin);
                    return outputStream;
                }
            }
        }
    }
}
