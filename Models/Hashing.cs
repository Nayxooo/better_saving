using System;
using System.IO;
using System.IO.Hashing;
using System.Security.Cryptography;
using System.Text;

namespace better_saving.Models
{
    public class Hashing
{
    /// <summary>
    /// Generates a hash for the given file using XxHash64.
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns></returns>
    public static string GetFileHash(string filePath)
    {
        try
        {
            // Use a buffer size of 8MB for efficient processing of large files
            const int bufferSize = 8 * 1024 * 1024; // 8MB buffer
            
            // Use XxHash64 for superior performance with large files
            var xxHash = new XxHash64();

            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, FileOptions.SequentialScan);
            byte[] buffer = new byte[bufferSize];
            int bytesRead;

            // Process the file in chunks
            while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                xxHash.Append(buffer.AsSpan(0, bytesRead));
            }

            // Get the hash and convert to string
            byte[] hashBytes = new byte[8]; // XxHash64 produces an 8-byte hash
            xxHash.GetHashAndReset(hashBytes);
            return Convert.ToHexString(hashBytes);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error hashing file {filePath}: {ex.Message}");
            return string.Empty; // Return an empty string in case of error
        }
    }

    /// <summary>
    /// Compares two files by their hashes.
    /// This method uses XxHash64 for efficient comparison.
    /// </summary>
    /// <param name="filePath1"></param>
    /// <param name="filePath2"></param>
    /// <returns></returns
    public static bool CompareFiles(string filePath1, string filePath2)
    {
        return GetFileHash(filePath1) == GetFileHash(filePath2);
    }
}
}