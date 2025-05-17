using System;
using System.Collections.Generic;

public class Backup
{
    /// <summary>
    /// Copies a file from a source path to a target path and measures the time taken.
    /// </summary>
    /// <param name="sourceFilePath">The full path of the source file to copy.</param>
    /// <param name="targetFilePath">The full path where the file should be copied to.</param>
    /// <returns>
    /// The time taken in milliseconds to copy the file, or -1 if the operation failed.
    /// </returns>
    public static int backupFile(string sourceFilePath, string targetFilePath)
    {
        // copy the file from source to target
        // keep track of the file size and time taken for the backup
        // return the time taken for the backup (-1 for failure)        if (string.IsNullOrEmpty(sourceFilePath) || string.IsNullOrEmpty(targetFilePath))
        try
        {
            // copy the file
            int startTime = Environment.TickCount;
            System.IO.File.Copy(sourceFilePath, targetFilePath, true);
            return Environment.TickCount - startTime; // Return time taken for backup
        }
        catch
        {
            return -1; // Indicate failure
        }
    }
}