using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace WitShells.DesignPatterns
{
    public class Packet
    {
        public string Header { get; set; } // can be used as an identifier
        public string Payload { get; set; } // main data content of the packet in json or compressed format
    }

    public static class PacketCompressionUtils
    {
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