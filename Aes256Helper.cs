using System;
using System.IO;
using System.Security.Cryptography;

namespace LPH_Edit_Viewer
{
    public static class Aes256Helper
    {
        // Метод для шифрования текста
        public static byte[] Encrypt(string plainText, byte[] key, byte[] iv)
        {
            // Проверяем входные данные
            if (plainText == null || plainText.Length <= 0)
                throw new ArgumentNullException(nameof(plainText));
            if (key == null || key.Length != 32) // Ключ должен быть 32 байта
                throw new ArgumentException("Key must be 32 bytes for AES-256.", nameof(key));
            if (iv == null || iv.Length != 16) // IV должен быть 16 байт
                throw new ArgumentException("IV must be 16 bytes.", nameof(iv));

            byte[] encrypted;

            // Создаем объект AES с заданными ключом и IV
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = key;
                aesAlg.IV = iv;

                // Создаем шифратор
                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                // Используем MemoryStream для хранения зашифрованных данных
                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            // Записываем данные в поток для шифрования
                            swEncrypt.Write(plainText);
                        }
                        encrypted = msEncrypt.ToArray();
                    }
                }
            }

            // Возвращаем зашифрованные байты
            return encrypted;
        }

        // Метод для дешифрования текста
        public static string Decrypt(byte[] cipherText, byte[] key, byte[] iv)
        {
            // Проверяем входные данные
            if (cipherText == null || cipherText.Length <= 0)
                throw new ArgumentNullException(nameof(cipherText));
            if (key == null || key.Length != 32)
                throw new ArgumentException("Key must be 32 bytes for AES-256.", nameof(key));
            if (iv == null || iv.Length != 16)
                throw new ArgumentException("IV must be 16 bytes.", nameof(iv));

            string plaintext = null;

            // Создаем объект AES с заданными ключом и IV
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = key;
                aesAlg.IV = iv;

                // Создаем дешифратор
                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                // Используем MemoryStream с зашифрованными данными
                using (MemoryStream msDecrypt = new MemoryStream(cipherText))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {
                            // Читаем расшифрованные данные из потока
                            plaintext = srDecrypt.ReadToEnd();
                        }
                    }
                }
            }

            return plaintext;
        }
    }
}
