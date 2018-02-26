using PgpCore;
using System;
using System.IO;
using Kurukuru;

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
                string privateKeyFilePath = Path.Combine(tempPath, "private.asc");
                string contentFilePath = Path.Combine(tempPath, "content.txt");
                string encryptedFilePath = Path.Combine(tempPath, "content_encrypted.pgp");
                string decryptedFilePath = Path.Combine(tempPath, "content_decrypted.txt");
                string username = null;
                string password = null;
                int strength = 4096;
                int certainty = 8;

                //Create temp direcory and test file
                Directory.CreateDirectory(tempPath);
                Console.WriteLine($"Created temp directory: {tempPath}");
                File.WriteAllText(contentFilePath, "Hello World PGPCore!");
                Console.WriteLine($"Created test file: {contentFilePath}");

                // Generate keys
                Spinner.Start("Generating keys...", () =>
                {
                    pgp.GenerateKey(publicKeyFilePath, privateKeyFilePath, username, password, strength, certainty);
                });
                Console.WriteLine($"Created public key: {publicKeyFilePath}");
                Console.WriteLine($"Created private key: {privateKeyFilePath}");

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
