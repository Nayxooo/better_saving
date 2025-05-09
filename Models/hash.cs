using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

public class Hashing
{
    public static string GetFileHash(string filePath)
    {
        try
        {
            // Use a buffer size of 4MB for efficient processing of large files
            const int bufferSize = 4 * 1024 * 1024; // 4MB buffer
            
            using (var sha256 = SHA256.Create())
            using (var stream = File.OpenRead(filePath))
            {
                byte[] buffer = new byte[bufferSize];
                int bytesRead;
                
                // Process the file in chunks
                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    sha256.TransformBlock(buffer, 0, bytesRead, null, 0);
                }
                
                // Complete the hash computation
                sha256.TransformFinalBlock([], 0, 0);
                
                // Convert the hash to a string
                var hashBytes = sha256.Hash;
                var sb = new StringBuilder(hashBytes?.Length * 2 ?? 0);
                if (hashBytes != null)
                {
                    foreach (var b in hashBytes)
                    {
                        sb.Append(b.ToString("x2"));
                    }
                }
                
                return sb.ToString();
            }
        }
        catch
        {
            return string.Empty; // Return an empty string in case of error
        }
    }

    public static bool CompareFiles(string filePath1, string filePath2)
    {
        return GetFileHash(filePath1) == GetFileHash(filePath2);
    }
}