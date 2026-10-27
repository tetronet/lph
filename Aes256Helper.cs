using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace LPH_Edit_Viewer
{
    public static class Aes256Helper
    {
        /// <summary>
        /// Session key size: 32 bytes for AES-256 and separate 32 bytes for HMAC-SHA256.
        /// </summary>
        public const int KEY_SIZE = 64;
        public const int SALT_SIZE = 16;
        /// <summary>
        /// PBKDF2-HMAC-SHA256 iterations (OWASP recommendation), makes every password guess expensive.
        /// </summary>
        public const int PBKDF2_ITERATIONS = 600_000;

        /// <summary>
        /// Random salt for a new session. The viewer generates it and sends it to the editor, so each session gets its own key.
        /// </summary>
        public static byte[] NewSalt()
        {
            return RandomNumberGenerator.GetBytes(SALT_SIZE);
        }

        /// <summary>
        /// Derives the session key from the password and the session salt with PBKDF2.
        /// </summary>
        public static byte[] DeriveKey(string password, byte[] salt)
        {
            return Rfc2898DeriveBytes.Pbkdf2(password, salt, PBKDF2_ITERATIONS, HashAlgorithmName.SHA256, KEY_SIZE);
        }

        public static byte[] Encrypt(string plainText, byte[] key)
        {
            CheckKey(key);
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] iv = RandomNumberGenerator.GetBytes(16);

            byte[] cipher;
            using (var aes = Aes.Create())
            {
                aes.Key = key[..32]; aes.IV = iv;
                using var enc = aes.CreateEncryptor();
                cipher = enc.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
            }

            byte[] macInput = new byte[iv.Length + cipher.Length];
            Buffer.BlockCopy(iv, 0, macInput, 0, iv.Length);
            Buffer.BlockCopy(cipher, 0, macInput, iv.Length, cipher.Length);
            byte[] mac = HMACSHA256.HashData(key[32..], macInput);

            byte[] result = new byte[iv.Length + cipher.Length + mac.Length];
            Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
            Buffer.BlockCopy(cipher, 0, result, iv.Length, cipher.Length);
            Buffer.BlockCopy(mac, 0, result, iv.Length + cipher.Length, mac.Length);
            return result;
        }

        public static string Decrypt(byte[] data, byte[] key)
        {
            CheckKey(key);
            if (data.Length < 16 + 16 + 32) throw new InvalidDataException("too short");

            byte[] iv = data[..16];
            byte[] mac = data[^32..];
            byte[] cipher = data[16..^32];

            byte[] macInput = new byte[iv.Length + cipher.Length];
            Buffer.BlockCopy(iv, 0, macInput, 0, iv.Length);
            Buffer.BlockCopy(cipher, 0, macInput, iv.Length, cipher.Length);
            byte[] expected = HMACSHA256.HashData(key[32..], macInput);

            if (!CryptographicOperations.FixedTimeEquals(mac, expected))
                throw new InvalidDataException("MAC mismatch");

            using var aes = Aes.Create();
            aes.Key = key[..32]; aes.IV = iv;
            using var dec = aes.CreateDecryptor();
            byte[] plain = dec.TransformFinalBlock(cipher, 0, cipher.Length);
            return Encoding.UTF8.GetString(plain);
        }

        private static void CheckKey(byte[] key)
        {
            if (key.Length != KEY_SIZE)
            {
                throw new ArgumentException($"key must be {KEY_SIZE} bytes, derive it with {nameof(DeriveKey)}", nameof(key));
            }
        }
    }
}
