using ExcelDatabaseImportTool.Interfaces.Services;
using System.Security.Cryptography;
using System.Text;
using System.IO;

namespace ExcelDatabaseImportTool.Services.Database
{
    public class EncryptionService : IEncryptionService
    {
        private readonly byte[] _key;
        private readonly byte[] _iv;

        public EncryptionService()
        {
            // For testing purposes, use a fixed key and IV
            // In production, these should be securely generated and stored
            _key = Encoding.UTF8.GetBytes("12345678901234567890123456789012"); // 32 bytes for AES-256
            _iv = Encoding.UTF8.GetBytes("1234567890123456"); // 16 bytes for AES
        }

        public string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
            {
                throw new ArgumentException("Plain text cannot be null or empty", nameof(plainText));
            }

            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV = _iv;

            using var encryptor = aes.CreateEncryptor();
            using var msEncrypt = new MemoryStream();
            using var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write);
            using var swEncrypt = new StreamWriter(csEncrypt);
            
            swEncrypt.Write(plainText);
            swEncrypt.Close();
            
            return Convert.ToBase64String(msEncrypt.ToArray());
        }

        public string Decrypt(string encryptedText)
        {
            if (string.IsNullOrEmpty(encryptedText))
            {
                throw new ArgumentException("Encrypted text cannot be null or empty", nameof(encryptedText));
            }

            byte[] cipherBytes;
            try
            {
                cipherBytes = Convert.FromBase64String(encryptedText);
            }
            catch (FormatException ex)
            {
                throw new FormatException(
                    "The encrypted text is not a valid Base-64 string. " +
                    "It may be corrupted or was not properly encrypted.", ex);
            }

            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV = _iv;

            using var decryptor = aes.CreateDecryptor();
            using var msDecrypt = new MemoryStream(cipherBytes);
            using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
            using var srDecrypt = new StreamReader(csDecrypt);
            
            return srDecrypt.ReadToEnd();
        }
    }
}