using System;
using System.Collections.Generic;

public class Backup
{
    public static int backupFile(string sourceFilePath, string targetFilePath)
    {
        // copy the file from source to target
        // keep track of the file size and time taken for the backup
        // return the time taken for the backup (-1 for failure)
        if (string.IsNullOrEmpty(sourceFilePath) || string.IsNullOrEmpty(targetFilePath))
        {
            return -1; // Indicate failure
        }
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