using PgpCore;
using System;
using System.IO;
using Kurukuru;
using System.Text;

namespace PGPEncryptConsoleApp
{
    class Program
    {
        static void Main()
        {
            using (PGP pgp = new PGP())
            {
                Console.WriteLine($"Welcome to PGPEncryptConsoleApp!");

                string tempPath = Path.Combine(Path.GetTempPath(), "pgpcore");
                string publicKeyFilePath = Path.Combine(tempPath, "public.asc");
                string publicKeyBase64FilePath = Path.Combine(tempPath, "public_base64.asc");
                string privateKeyFilePath = Path.Combine(tempPath, "private.asc");
                string privateKeyBase64FilePath = Path.Combine(tempPath, "private_base64.asc");
                string contentFilePath = Path.Combine(tempPath, "content.txt");
                string encryptedFilePath = Path.Combine(tempPath, "content_encrypted.pgp");
                string decryptedFilePath = Path.Combine(tempPath, "content_decrypted.txt");
                string username = null;
                int strength = 4096;
                int certainty = 8;

                //Create temp direcory and test file
                Directory.CreateDirectory(tempPath);
                Console.WriteLine($"Created temp directory: {tempPath}");
                File.WriteAllText(contentFilePath, "Hello World PGPCore!");
                Console.WriteLine($"Created test file: {contentFilePath}");

                // Create a password
                Console.WriteLine($"Enter a password or press enter to not use a password");
                string password = ReadLine.ReadPassword();

                // Generate keys
                Spinner.Start("Generating keys...", () =>
                {
                    pgp.GenerateKey(publicKeyFilePath, privateKeyFilePath, username, password, strength, certainty);
                });
                string publicKey = File.ReadAllText(publicKeyFilePath);
                File.WriteAllText(publicKeyBase64FilePath, Convert.ToBase64String(Encoding.UTF8.GetBytes(publicKey)));
                Console.WriteLine($"Created public key: {publicKeyFilePath}");
                Console.WriteLine($"Converted public key to base64: {publicKeyBase64FilePath}");

                Console.WriteLine($"Created private key: {privateKeyFilePath}");
                string privateKey = File.ReadAllText(privateKeyFilePath);
                File.WriteAllText(privateKeyBase64FilePath, Convert.ToBase64String(Encoding.UTF8.GetBytes(privateKey)));
                Console.WriteLine($"Created private key: {privateKeyFilePath}");
                Console.WriteLine($"Converted private key to base64: {privateKeyBase64FilePath}");

                // Encrypt file
                pgp.EncryptFile(contentFilePath, encryptedFilePath, publicKeyFilePath, true, true);
                Console.WriteLine($"Encrypted test file: {encryptedFilePath}");

                // Decrypt file
                pgp.DecryptFile(encryptedFilePath, decryptedFilePath, privateKeyFilePath, password);
                Console.WriteLine($"Decrypted test file: {decryptedFilePath}");

                Console.WriteLine("Press any key to exit");
                Console.ReadLine();
            }
        }
    }
}
