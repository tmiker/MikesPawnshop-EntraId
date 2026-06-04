using Accounts.API.Abstractions;
using System.Security.Cryptography;
using System.Text;

namespace Accounts.API.Crypto
{
    public class EncryptionHelper : IEncryptionHelper
    {
        /// <summary>
        /// Encrypts the input string using AES-256.
        /// </summary>
        public string Encrypt(string plainText, string aesKey, string aesIV)
        {
            if (string.IsNullOrEmpty(plainText)) return string.Empty;

            using var aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(aesKey);
            aes.IV = Encoding.UTF8.GetBytes(aesIV);

            using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream();
            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            using (var sw = new StreamWriter(cs))
            {
                sw.Write(plainText);
            }
            return Convert.ToBase64String(ms.ToArray());
        }

        /// <summary>
        /// Decrypts the input string using AES-256.
        /// </summary>
        public string Decrypt(string cipherText, string aesKey, string aesIV)
        {
            if (string.IsNullOrEmpty(cipherText)) return string.Empty;

            using var aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(aesKey);
            aes.IV = Encoding.UTF8.GetBytes(aesIV);

            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream(Convert.FromBase64String(cipherText));
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var sr = new StreamReader(cs);
            return sr.ReadToEnd();
        }
    }
}
