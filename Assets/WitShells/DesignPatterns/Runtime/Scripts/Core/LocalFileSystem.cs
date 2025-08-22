using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace WitShells.DesignPatterns.Core
{
    public static class LocalFileSystem
    {
        public static IEnumerable<string> LoadAllFileNames(string path, string extension = ".json")
        {
            if (!Directory.Exists(path)) yield break;

            foreach (var file in Directory.GetFiles(path, $"*{extension}"))
            {
                yield return Path.GetFileNameWithoutExtension(file);
            }
        }

        public static async Task SaveFileAsync(string savePath, string fileName, string password, string jsonData, string extension = ".json")
        {
            // Compress the JSON data before encryption
            byte[] compressedJson = CompressStringGZip(jsonData);
            string compressedBase64 = Convert.ToBase64String(compressedJson);
            string encryptedJson = Encrypt(compressedBase64, password);
            string fullPath = Path.Combine(savePath, fileName + extension);

            if (!Directory.Exists(savePath))
            {
                Directory.CreateDirectory(savePath);
            }

            await File.WriteAllTextAsync(fullPath, encryptedJson);
        }

        public static async Task<string> LoadFileAsync(string savePath, string fileName, string password, string extension = ".json")
        {
            string fullPath = Path.Combine(savePath, fileName + extension);
            if (!File.Exists(fullPath))
            {
                return null;
            }

            string encryptedJson = await File.ReadAllTextAsync(fullPath);
            string compressedBase64 = Decrypt(encryptedJson, password);
            byte[] compressedJson = Convert.FromBase64String(compressedBase64);
            return DecompressStringGZip(compressedJson);
        }

        // AES encryption with password
        public static byte[] GetKey(string password)
        {
            // Use SHA256 to create a 256-bit key from the password
            using (SHA256 sha = SHA256.Create())
            {
                return sha.ComputeHash(Encoding.UTF8.GetBytes(password));
            }
        }

        public static string Encrypt(string plainText, string password)
        {
            byte[] key = GetKey(password);
            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.GenerateIV();
                using (var encryptor = aes.CreateEncryptor())
                {
                    byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
                    byte[] cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

                    // Prepend IV for decryption
                    byte[] result = new byte[aes.IV.Length + cipherBytes.Length];
                    Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
                    Buffer.BlockCopy(cipherBytes, 0, result, aes.IV.Length, cipherBytes.Length);
                    return Convert.ToBase64String(result);
                }
            }
        }

        public static string Decrypt(string cipherText, string password)
        {
            byte[] key = GetKey(password);
            byte[] fullCipher = Convert.FromBase64String(cipherText);
            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                byte[] iv = new byte[aes.BlockSize / 8];
                byte[] cipherBytes = new byte[fullCipher.Length - iv.Length];
                Buffer.BlockCopy(fullCipher, 0, iv, 0, iv.Length);
                Buffer.BlockCopy(fullCipher, iv.Length, cipherBytes, 0, cipherBytes.Length);
                aes.IV = iv;
                using (var decryptor = aes.CreateDecryptor())
                {
                    byte[] plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
                    return Encoding.UTF8.GetString(plainBytes);
                }
            }
        }

        /// <summary>
        /// Compresses a string using GZip and returns the compressed byte array.
        /// </summary>
        public static byte[] CompressStringGZip(string input)
        {
            if (string.IsNullOrEmpty(input)) return Array.Empty<byte>();
            var bytes = Encoding.UTF8.GetBytes(input);
            using (var output = new MemoryStream())
            {
                using (var gzip = new System.IO.Compression.GZipStream(output, System.IO.Compression.CompressionLevel.Optimal, true))
                {
                    gzip.Write(bytes, 0, bytes.Length);
                }
                return output.ToArray();
            }
        }

        /// <summary>
        /// Decompresses a GZip byte array back into a string.
        /// </summary>
        public static string DecompressStringGZip(byte[] compressedBytes)
        {
            if (compressedBytes == null || compressedBytes.Length == 0) return string.Empty;

            using (var input = new MemoryStream(compressedBytes))
            {
                using (var gzip = new System.IO.Compression.GZipStream(input, System.IO.Compression.CompressionMode.Decompress))
                {
                    using (var output = new MemoryStream())
                    {
                        gzip.CopyTo(output);
                        return Encoding.UTF8.GetString(output.ToArray());
                    }
                }
            }
        }
    }
}