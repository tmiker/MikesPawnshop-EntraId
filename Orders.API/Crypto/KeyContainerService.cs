using Orders.API.DTOs;
using Orders.API.Abstractions;

namespace Orders.API.Crypto
{
    public class KeyContainerService : IKeyContainerService
    {
        private readonly IRsaAsymmetricKeyContainerManager _rsaKeyContainerManager;
        private readonly IAesSymmetricEncryptionManager _aesEncryptor;
        private readonly IConfiguration _config;
        private readonly ILogger<KeyContainerService> _logger;

        public KeyContainerService(
            IRsaAsymmetricKeyContainerManager rsaKeyContainerManager,
            IAesSymmetricEncryptionManager aesEncryptor,
            IConfiguration config,
            ILogger<KeyContainerService> logger)
        {
            _rsaKeyContainerManager = rsaKeyContainerManager;
            _aesEncryptor = aesEncryptor;
            _config = config;
            _logger = logger;
        }

        public (bool IsSuccess, KeyContainerResponseDTO? KeyContainerResponse, string? ErrorMessage) GetPublicKeyForSpecifiedContainerAsync()
        {
            string keyContainerName = Guid.NewGuid().ToString();
            _logger.LogInformation("*** {this}: Accounts Key Container Name generated: {key} ***", this.GetType().Name, keyContainerName);

            string publicKey = _rsaKeyContainerManager.GetPublicKeyForContainerWithName(keyContainerName);
            // Return encrypted public key by using aes encryption
            string? aesKey = _config["IntAcctsAesSymEncryption_Key"];
            string? iv = _config["IntAcctsAesSymEncryption_IV"];
            string encryptedPublicKey = _aesEncryptor.EncryptSymmetric(publicKey, aesKey!, iv!);

            KeyContainerResponseDTO keyContainerResponseDTO = new KeyContainerResponseDTO() { EncryptedPublicKey = encryptedPublicKey, KeyContainerName = keyContainerName };
            // _logger.LogInformation("{this}: KeyContainerResponseDTO generated to return to public api. Encrypted Public Key: {key}", this.GetType().Name, encryptedPublicKey);  // *** REMOVE ****
            return (true, keyContainerResponseDTO, null);
        }
    }
}
