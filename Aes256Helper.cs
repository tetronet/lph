using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace LPH_Edit_Viewer
{
    public static class Aes256Helper
    {
        public static byte[] Encrypt(string plainText, byte[] key)
        {
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] iv = RandomNumberGenerator.GetBytes(16);

            byte[] cipher;
            using (var aes = Aes.Create())
            {
                aes.Key = key; aes.IV = iv;
                using var enc = aes.CreateEncryptor();
                cipher = enc.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
            }

            byte[] macInput = new byte[iv.Length + cipher.Length];
            Buffer.BlockCopy(iv, 0, macInput, 0, iv.Length);
            Buffer.BlockCopy(cipher, 0, macInput, iv.Length, cipher.Length);
            byte[] mac = HMACSHA256.HashData(key, macInput);

            byte[] result = new byte[iv.Length + cipher.Length + mac.Length];
            Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
            Buffer.BlockCopy(cipher, 0, result, iv.Length, cipher.Length);
            Buffer.BlockCopy(mac, 0, result, iv.Length + cipher.Length, mac.Length);
            return result;
        }

        public static string Decrypt(byte[] data, byte[] key)
        {
            if (data.Length < 16 + 16 + 32) throw new InvalidDataException("too short");

            byte[] iv = data[..16];
            byte[] mac = data[^32..];
            byte[] cipher = data[16..^32];

            byte[] macInput = new byte[iv.Length + cipher.Length];
            Buffer.BlockCopy(iv, 0, macInput, 0, iv.Length);
            Buffer.BlockCopy(cipher, 0, macInput, iv.Length, cipher.Length);
            byte[] expected = HMACSHA256.HashData(key, macInput);

            if (!CryptographicOperations.FixedTimeEquals(mac, expected))
                throw new InvalidDataException("MAC mismatch");

            using var aes = Aes.Create();
            aes.Key = key; aes.IV = iv;
            using var dec = aes.CreateDecryptor();
            byte[] plain = dec.TransformFinalBlock(cipher, 0, cipher.Length);
            return Encoding.UTF8.GetString(plain);
        }
    }
}
