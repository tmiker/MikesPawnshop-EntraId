namespace Orders.API.Abstractions
{
    public interface IEncryptionHelper
    {
        string Encrypt(string plainText, string aesKey, string aesIV);
        string Decrypt(string cipherText, string aesKey, string aesIV);
    }
}
