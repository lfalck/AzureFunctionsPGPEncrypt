[![license](https://img.shields.io/github/license/lfalck/AzureFunctionsPGPEncrypt.svg)]()
# AzureFunctionsPGPEncrypt

Azure function which performs PGP encryption using [PgpCore](https://github.com/mattosaurus/PgpCore). The public key can be passed as a query parameter, stored in an environment variable, or in Azure Key Vault and accessed using [Managed Service Identity](https://docs.microsoft.com/en-us/azure/app-service/app-service-managed-service-identity).

# Get started
* Make a request to the function with the unencrypted data in the body
* Option 1: Pass the Base64 encoded public key as a query parameter called public-key
* Option 2: Store the Base64 encoded public key in an environment variable and pass the variable name as a query parameter called public-key-environment-variable
* Option 3: Store the Base64 encoded public key in Azure Key Vault and pass the Key Vault Secret Identifier (e.g. https://vaultname.vault.azure.net/secrets/secretname/version) as a query parameter called public-key-secret-id

## Enabling Managed Service Identity for use with Azure Key Vault
* Enable Managed Service Identity for your Function App
* Add an Access Policy in Key Vault which gives Get permissions for Secrets to your Function App Principal
* Add a Base64 encoded public key as an Azure Key Vault Secret

# Caching
To increase performance the function does some simple caching to avoid fetching from Azure Key Vault on each invocation if that option is used. The Key Vault Secret Identifier is used as a key in the cache, which means that if you include the secret version in the identifier new versions will always be fetched from Key Vault.
