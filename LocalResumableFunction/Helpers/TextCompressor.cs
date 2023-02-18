using System.IO.Compression;
using System.Text;

namespace LocalResumableFunction.Helpers;

public class TextCompressor
{
    public static byte[] CompressString(string text)
    {
        return Compress(Encoding.ASCII.GetBytes(text));
    }

    public static byte[] Compress(byte[] bytes)
    {
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