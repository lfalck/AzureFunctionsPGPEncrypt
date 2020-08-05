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

namespace AzureFunctionsPGPEncrypt
{
    public static class PGPEncrypt
    {
        private const string PublicKeyEnvironmentVariable = "pgp-public-key";

        [FunctionName(nameof(PGPEncrypt))]
        public static async Task<IActionResult> RunAsync(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]
        HttpRequest req, ILogger log)
        {
            log.LogInformation($"C# HTTP trigger function {nameof(PGPEncrypt)} processed a request.");

            string publicKeyBase64 = Environment.GetEnvironmentVariable(PublicKeyEnvironmentVariable);

            if (string.IsNullOrEmpty(publicKeyBase64))
            {
                return new BadRequestObjectResult($"Please add a base64 encoded public key to an environment variable called {PublicKeyEnvironmentVariable}");
            }

            byte[] publicKeyBytes = Convert.FromBase64String(publicKeyBase64);
            string publicKey = Encoding.UTF8.GetString(publicKeyBytes);
            req.EnableBuffering(); //Make RequestBody Stream seekable
            Stream encryptedData = await EncryptAsync(req.Body, publicKey);

            return new OkObjectResult(encryptedData);
        }

        private static async Task<Stream> EncryptAsync(Stream inputStream, string publicKey)
        {
            using (PGP pgp = new PGP())
            {
                Stream outputStream = new MemoryStream();

                using (inputStream)
                using (Stream publicKeyStream = GenerateStreamFromString(publicKey))
                {
                    await pgp.EncryptStreamAsync(inputStream, outputStream, publicKeyStream, true, true);
                    outputStream.Seek(0, SeekOrigin.Begin);
                    return outputStream;
                }
            }
        }

        private static Stream GenerateStreamFromString(string s)
        {
            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }
    }
}
