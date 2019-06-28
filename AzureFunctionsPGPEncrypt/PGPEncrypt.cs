using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using PgpCore;
using System.Threading.Tasks;
using Microsoft.Azure.KeyVault.Models;
using System.Net.Http;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Azure.KeyVault;
using System;
using System.Text;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.Extensions.Logging;

namespace AzureFunctionsPGPEncrypt
{
    public static class PGPEncrypt
    {
        private static readonly HttpClient client = new HttpClient();
        private static ConcurrentDictionary<string, string> secrets = new ConcurrentDictionary<string, string>();

        [FunctionName(nameof(PGPEncrypt))]
        public static async Task<IActionResult> RunAsync(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]
        HttpRequest req, ILogger log)
        {
            log.LogInformation($"C# HTTP trigger function {nameof(PGPEncrypt)} processed a request.");

            string publicKeyBase64 = req.Query["public-key"];
            string publicKeyEnvironmentVariable = req.Query["public-key-environment-variable"];
            string publicKeySecretId = req.Query["public-key-secret-id"];

            if (publicKeyBase64 == null && publicKeyEnvironmentVariable == null && publicKeySecretId == null)
            {
                return new BadRequestObjectResult("Please pass a base64 encoded public key, an environment variable name, or a key vault secret identifier on the query string");
            }

            if (publicKeyBase64 == null && publicKeyEnvironmentVariable != null)
            {
                publicKeyBase64 = Environment.GetEnvironmentVariable(publicKeyEnvironmentVariable);
            }

            if (publicKeyBase64 == null && publicKeySecretId != null)
            {
                try
                {
                    publicKeyBase64 = await GetPublicKeyAsync(publicKeySecretId);
                }
                catch (KeyVaultErrorException e) when (e.Body.Error.Code == "SecretNotFound")
                {
                    return new NotFoundResult();
                }
                catch (KeyVaultErrorException e) when (e.Body.Error.Code == "Forbidden")
                {
                    return new UnauthorizedResult();
                }
            }
            byte[] data = Convert.FromBase64String(publicKeyBase64);
            string publicKey = Encoding.UTF8.GetString(data);
            req.EnableRewind(); //Make RequestBody Stream seekable
            Stream encryptedData = await EncryptAsync(req.Body, publicKey);

            return new OkObjectResult(encryptedData);
        }

        private static async Task<string> GetPublicKeyAsync(string secretIdentifier)
        {
            if (!secrets.ContainsKey(secretIdentifier))
            {
                var azureServiceTokenProvider = new AzureServiceTokenProvider();
                var authenticationCallback = new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback);
                var kvClient = new KeyVaultClient(authenticationCallback, client);

                SecretBundle secretBundle = await kvClient.GetSecretAsync(secretIdentifier);
                secrets[secretIdentifier] = secretBundle.Value;
            }
            return secrets[secretIdentifier];
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
