using System.Diagnostics;
using System.Text;

namespace CryptoSoft;

public class FileManager(string filePath, string key)
{
    private string FilePath { get; } = filePath;
    private string Key { get; } = key;
    private static readonly byte[] Header = Encoding.UTF8.GetBytes("[CRYPT]");

    public int TransformFile()
    {
        try
        {
            var fileBytes = File.ReadAllBytes(FilePath);
            var keyBytes = Encoding.UTF8.GetBytes(Key);
            byte[] result;

            Stopwatch stopwatch = Stopwatch.StartNew();

            if (HasHeader(fileBytes))
            {
                Console.WriteLine($"Decrypting : {Path.GetFileName(FilePath)}");
                var encryptedContent = fileBytes.Skip(Header.Length).ToArray();
                result = XorMethod(encryptedContent, keyBytes);
            }
            else
            {
                Console.WriteLine($"Encrypting : {Path.GetFileName(FilePath)}");
                var encrypted = XorMethod(fileBytes, keyBytes);
                result = Header.Concat(encrypted).ToArray();
            }

            File.WriteAllBytes(FilePath, result);

            stopwatch.Stop();
            return (int)stopwatch.ElapsedMilliseconds;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error : {e.Message}");
            return -5;
        }
    }

    private static bool HasHeader(byte[] data)
    {
        if (data.Length < Header.Length) return false;
        return Header.SequenceEqual(data.Take(Header.Length));
    }

    private static byte[] XorMethod(IReadOnlyList<byte> data, IReadOnlyList<byte> key)
    {
        var result = new byte[data.Count];
        for (int i = 0; i < data.Count; i++)
        {
            result[i] = (byte)(data[i] ^ key[i % key.Count]);
        }
        return result;
    }
}
