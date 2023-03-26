using System.IO.Compression;
using System.Text;

namespace ResumableFunctions.Core.Helpers;

public class TextCompressor
{
    public static byte[] CompressString(string text)
    {
        if (text != null) return Compress(Encoding.ASCII.GetBytes(text));
        return null;
    }

    public static byte[] Compress(byte[] bytes)
    {
        if (bytes == null) return null;
        using (var memoryStream = new MemoryStream())
        {
            using (var gzipStream = new GZipStream(memoryStream, CompressionLevel.Optimal))
            {
                gzipStream.Write(bytes, 0, bytes.Length);
            }

            return memoryStream.ToArray();
        }
    }

    public static string DecompressString(byte[] bytes)
    {
        if (bytes == null) return null;
        return Encoding.ASCII.GetString(Decompress(bytes));
    }

    public static byte[] Decompress(byte[] bytes)
    {
        using (var memoryStream = new MemoryStream(bytes))
        {
            using (var outputStream = new MemoryStream())
            {
                using (var decompressStream = new GZipStream(memoryStream, CompressionMode.Decompress))
                {
                    decompressStream.CopyTo(outputStream);
                }

                return outputStream.ToArray();
            }
        }
    }
}