using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text;
using PgpCore;
using Org.BouncyCastle.Bcpg.OpenPgp;

namespace AzureFunctionsPGPEncrypt
{
    public static class PGPEncryptAndSign
    {
        [FunctionName(nameof(PGPEncryptAndSign))]
        public static async Task<IActionResult> RunAsync([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req, ILogger log)
        {
            log.LogInformation($"C# HTTP trigger function {nameof(PGPEncryptAndSign)} processed a request.");

            string publicKeyBase64 = Environment.GetEnvironmentVariable("pgp-public-key");
            string privateKeySignBase64 = Environment.GetEnvironmentVariable("pgp-private-key-sign");
            string passPhraseSign = Environment.GetEnvironmentVariable("pgp-passphrase-sign");

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
            using (PGP pgp = new PGP())
            {
                Stream outputStream = new MemoryStream();

                using (inputStream)
                using (Stream publicKeyStream = publicKey.ToStream())
                using (Stream privateKeyStream = privateKey.ToStream())

                {
                    await pgp.EncryptStreamAndSignAsync(inputStream, outputStream, publicKeyStream, privateKeyStream, passPhrase, true, true);
                    outputStream.Seek(0, SeekOrigin.Begin);
                    return outputStream;
                }
            }
        }
    }
}
