using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace WitShells.DesignPatterns
{
    /// <summary>
    /// A lightweight data transfer container used to send structured messages
    /// between systems (e.g. over WebSocket, local IPC, or within the same process).
    /// </summary>
    /// <remarks>
    /// The <see cref="Header"/> acts as a content-type descriptor (e.g. <c>"gzip"</c>) and
    /// the <see cref="Payload"/> carries the actual data, which may be plain JSON or
    /// a GZip-compressed Base64 string after calling
    /// <see cref="PacketCompressionUtils.CompressPayload"/>.
    /// </remarks>
    public class Packet
    {
        /// <summary>
        /// An optional identifier or content-type hint for the payload
        /// (e.g. <c>"gzip"</c> when the payload is compressed, or an event name).
        /// </summary>
        public string Header { get; set; }

        /// <summary>
        /// The main data content of the packet, either raw JSON or a
        /// Base64-encoded GZip-compressed string.
        /// </summary>
        public string Payload { get; set; }
    }

    /// <summary>
    /// Utility class providing GZip compression and decompression helpers for <see cref="Packet"/> payloads.
    /// Use these methods to reduce the wire size of large JSON payloads before sending.
    /// </summary>
    public static class PacketCompressionUtils
    {
        /// <summary>
        /// GZip-compresses the <see cref="Packet.Payload"/> in-place and encodes it as Base64.
        /// Sets <see cref="Packet.Header"/> to <c>"gzip"</c> to signal the encoding.
        /// </summary>
        /// <param name="packet">The packet whose payload should be compressed.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="packet"/> is null.</exception>
        public static void CompressPayload(Packet packet)
        {
            if (packet == null) throw new ArgumentNullException(nameof(packet));
            if (string.IsNullOrEmpty(packet.Payload)) return;

            var bytes = Encoding.UTF8.GetBytes(packet.Payload);
            using var input = new MemoryStream(bytes);
            using var output = new MemoryStream();
            using (var gzip = new GZipStream(output, CompressionLevel.Optimal, leaveOpen: true))
            {
                input.CopyTo(gzip);
            }
            packet.Payload = Convert.ToBase64String(output.ToArray());
            packet.Header = "gzip";
        }

        /// <summary>
        /// Decompresses a GZip Base64-encoded <see cref="Packet.Payload"/> back to its original string.
        /// Clears <see cref="Packet.Header"/> after decompression.
        /// </summary>
        /// <param name="packet">The packet whose payload should be decompressed.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="packet"/> is null.</exception>
        public static void DecompressPayload(Packet packet)
        {
            if (packet == null) throw new ArgumentNullException(nameof(packet));
            if (string.IsNullOrEmpty(packet.Payload)) return;

            var compressed = Convert.FromBase64String(packet.Payload);
            using var input = new MemoryStream(compressed);
            using var gzip = new GZipStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();
            gzip.CopyTo(output);
            packet.Payload = Encoding.UTF8.GetString(output.ToArray());
            packet.Header = null;
        }

        /// <summary>
        /// Compresses a plain-text string using GZip and returns it as a Base64 string.
        /// </summary>
        /// <param name="text">The text to compress.</param>
        /// <returns>A Base64-encoded GZip-compressed representation of <paramref name="text"/>,
        /// or the original string if empty.</returns>
        public static string CompressToBase64(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            var bytes = Encoding.UTF8.GetBytes(text);
            using var output = new MemoryStream();
            using (var gzip = new GZipStream(output, CompressionLevel.Optimal, leaveOpen: true))
            {
                gzip.Write(bytes, 0, bytes.Length);
            }
            return Convert.ToBase64String(output.ToArray());
        }

        /// <summary>
        /// Decompresses a Base64-encoded GZip string back to its original plain text.
        /// </summary>
        /// <param name="base64">The Base64-encoded GZip string to decompress.</param>
        /// <returns>The original plain-text string, or the input if empty.</returns>
        public static string DecompressFromBase64(string base64)
        {
            if (string.IsNullOrEmpty(base64)) return base64;
            var compressed = Convert.FromBase64String(base64);
            using var input = new MemoryStream(compressed);
            using var gzip = new GZipStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();
            gzip.CopyTo(output);
            return Encoding.UTF8.GetString(output.ToArray());
        }
    }
}