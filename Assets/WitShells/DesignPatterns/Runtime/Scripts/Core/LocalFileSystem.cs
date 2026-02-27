using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace WitShells.DesignPatterns.Core
{
    /// <summary>
    /// A static async helper for persisting data to the local file system with
    /// <b>AES-256 encryption</b> and <b>GZip compression</b>.
    /// </summary>
    /// <remarks>
    /// Typical use-case: save-game data, player preferences, or any sensitive runtime data
    /// that should not be readable as plain text on disk.<br/>
    /// All read/write operations are <c>async</c> to avoid blocking the Unity main thread.
    /// </remarks>
    public static class LocalFileSystem
    {
        /// <summary>
        /// Returns the file names (without extensions) of all files with the given extension
        /// found under <paramref name="path"/>.
        /// </summary>
        /// <param name="path">Directory path to search.</param>
        /// <param name="extension">File extension filter (default <c>".json"</c>).</param>
        /// <returns>Enumerable of file names without extension.</returns>
        public static IEnumerable<string> LoadAllFileNames(string path, string extension = ".json")
        {
            if (!Directory.Exists(path)) yield break;

            foreach (var file in Directory.GetFiles(path, $"*{extension}"))
            {
                yield return Path.GetFileNameWithoutExtension(file);
            }
        }

        /// <summary>
        /// Asynchronously compresses (GZip) and encrypts (AES-256) <paramref name="jsonData"/>
        /// then writes it to <c>&lt;savePath&gt;/&lt;fileName&gt;&lt;extension&gt;</c>.
        /// The directory is created if it does not exist.
        /// </summary>
        /// <param name="savePath">Target directory path.</param>
        /// <param name="fileName">File name without extension.</param>
        /// <param name="password">Encryption password (SHA-256 derived key).</param>
        /// <param name="jsonData">The raw content to persist.</param>
        /// <param name="extension">File extension (default <c>".json"</c>).</param>
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

        /// <summary>
        /// Asynchronously reads, decrypts (AES-256), and decompresses (GZip) a file saved by
        /// <see cref="SaveFileAsync"/>.
        /// </summary>
        /// <param name="savePath">Directory containing the file.</param>
        /// <param name="fileName">File name without extension.</param>
        /// <param name="password">The password used when the file was saved.</param>
        /// <param name="extension">File extension (default <c>".json"</c>).</param>
        /// <returns>The original plain-text data, or <c>null</c> if the file does not exist.</returns>
        /// <exception cref="LocalFileSystemException">Thrown when decryption fails (wrong password or corrupt file).</exception>
        public static async Task<string> LoadFileAsync(string savePath, string fileName, string password, string extension = ".json")
        {
            try
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
            catch
            {
                throw new LocalFileSystemException($"Failed to load or decrypt file with your password: {fileName}");
            }
        }

        /// <summary>
        /// Asynchronously deletes a file after verifying the password via decryption.
        /// </summary>
        /// <param name="savePath">Directory containing the file.</param>
        /// <param name="fileName">File name without extension.</param>
        /// <param name="password">Password used to verify ownership before deletion.</param>
        /// <param name="extension">File extension (default <c>".json"</c>).</param>
        /// <returns><c>true</c> on successful deletion.</returns>
        /// <exception cref="LocalFileSystemException">Thrown when the file is not found or decryption fails.</exception>
        public static async Task<bool> DeleteFileAsync(string savePath, string fileName, string password, string extension = ".json")
        {
            try
            {
                string fullPath = Path.Combine(savePath, fileName + extension);
                if (!File.Exists(fullPath))
                {
                    throw new LocalFileSystemException($"File not found: {fileName}");
                }

                // First verify the password by attempting to decrypt the file
                string encryptedJson = await File.ReadAllTextAsync(fullPath);
                string compressedBase64 = Decrypt(encryptedJson, password);

                // If decryption succeeded (no exception thrown), delete the file
                File.Delete(fullPath);
                return true;
            }
            catch (LocalFileSystemException ex)
            {
                throw ex; // Re-throw specific exceptions
            }
            catch (Exception ex)
            {
                throw new LocalFileSystemException($"Failed to delete file {fileName}: {ex.Message}");
            }
        }

        /// <summary>
        /// Derives a 256-bit AES key from a plain-text password using SHA-256.
        /// </summary>
        /// <param name="password">The user-supplied password.</param>
        /// <returns>A 32-byte key array.</returns>
        public static byte[] GetKey(string password)
        {
            // Use SHA256 to create a 256-bit key from the password
            using (SHA256 sha = SHA256.Create())
            {
                return sha.ComputeHash(Encoding.UTF8.GetBytes(password));
            }
        }

        /// <summary>
        /// AES-256 encrypts <paramref name="plainText"/>, prepends the IV, and returns a Base64 string.
        /// </summary>
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

        /// <summary>
        /// Decrypts a Base64 AES-256 cipher string produced by <see cref="Encrypt"/>.
        /// </summary>
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

    /// <summary>
    /// Exception thrown by <see cref="LocalFileSystem"/> when a file operation fails
    /// (e.g. wrong password, missing file, or corrupt data).
    /// </summary>
    public class LocalFileSystemException : Exception
    {
        /// <summary>Creates a new <see cref="LocalFileSystemException"/> with the given message.</summary>
        public LocalFileSystemException(string message) : base(message) { }
    }
}