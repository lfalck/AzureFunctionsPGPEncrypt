[![license](https://img.shields.io/github/license/lfalck/AzureFunctionsPGPEncrypt.svg)]()
# AzureFunctionsPGPEncrypt

Azure function which performs PGP encryption using [PgpCore](https://github.com/mattosaurus/PgpCore). The public key can be stored in an environment variable, or in Azure Key Vault and accessed using [Key Vault references](https://docs.microsoft.com/en-us/azure/app-service/app-service-key-vault-references).

# Get started
* Make a request to the function with the unencrypted data in the body
* Option 1: Store the Base64 encoded public key in an environment variable called pgp-public-key.
* Option 2: Store the Base64 encoded public key in Azure Key Vault and add a Key vault reference in an environment variable called pgp-public-key.

## Key generation
Option 1: Use a program such as [GPG](https://gnupg.org/) or [GPW4Win](https://www.gpg4win.org/).  
Option 2: Use the console app **PGPEncryptConsoleApp** in the repo
