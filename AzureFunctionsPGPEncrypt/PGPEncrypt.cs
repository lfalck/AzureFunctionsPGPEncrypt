using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using PgpCore;
using System.Threading.Tasks;
using Microsoft.Azure.KeyVault.Models;
using System.Net.Http;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Azure.KeyVault;
using System;
using System.Text;
using System.Collections.Concurrent;

namespace AzureFunctionsPGPEncrypt
{
    public static class PGPEncrypt
    {
        private static HttpClient client = new HttpClient();
        private static ConcurrentDictionary<string, string> secrects = new ConcurrentDictionary<string, string>();

        [FunctionName(nameof(PGPEncrypt))]
        public static async Task<IActionResult> RunAsync(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]
        HttpRequest req, TraceWriter log)
        {
            log.Info($"C# HTTP trigger function {nameof(PGPEncrypt)} processed a request.");

            string publicKeySecretId = req.Query["publickeysecretid"];

            if (publicKeySecretId == null)
            {
                return new BadRequestObjectResult("Please pass a public key secret identifier on the query string");
            }

            string publicKey;
            try
            {
                publicKey = await GetPublicKeyAsync(publicKeySecretId);
            }
            catch (KeyVaultErrorException e) when (e.Body.Error.Code == "SecretNotFound")
            {
                return new NotFoundResult();
            }
            catch (KeyVaultErrorException e) when (e.Body.Error.Code == "Forbidden")
            {
                return new UnauthorizedResult();
            }

            Stream encryptedData = await EncryptAsync(req.Body, publicKey);

            return new OkObjectResult(encryptedData);
        }

        private static async Task<string> GetPublicKeyAsync(string secretIdentifier)
        {
            if (!secrects.ContainsKey(secretIdentifier))
            {
                var azureServiceTokenProvider = new AzureServiceTokenProvider();
                var authenticationCallback = new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback);
                var kvClient = new KeyVaultClient(authenticationCallback, client);

                SecretBundle secretBundle = await kvClient.GetSecretAsync(secretIdentifier);
                byte[] data = Convert.FromBase64String(secretBundle.Value);
                secrects[secretIdentifier] = Encoding.UTF8.GetString(data);
            }
            return secrects[secretIdentifier];
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
