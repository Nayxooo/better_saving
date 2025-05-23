using Microsoft.Extensions.Configuration;
using System;

namespace CryptoSoft;

public static class Program
{
    public static void Main(string[] args)
    {
        try
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Use: CryptoSoft.exe <filepath> <configpath>");
                Environment.Exit(-1);
            }

            string filePath = args[0];
            string configPath = args[1];

            if (!File.Exists(filePath))
            {
                Console.WriteLine($"File to encrypting '{filePath}' not found");
                Environment.Exit(-2);
            }

            if (!File.Exists(configPath))
            {
                Console.WriteLine($"Configuration file '{configPath}' not found");
                Environment.Exit(-3);
            }            // Lire la clé depuis appsettings.json
            var config = new ConfigurationBuilder()
                .AddJsonFile(configPath)
                .Build();

            string? key = config["EncryptionKey"];
            if (string.IsNullOrWhiteSpace(key) || key.Length < 8)
            {
                Console.WriteLine("Invalid Key. (Champ 'EncryptionKey' empty or too short)");
                Environment.Exit(-4);
            }

            var fileManager = new FileManager(filePath, key);
            int elapsed = fileManager.TransformFile();

            Console.WriteLine($"File processed in {elapsed} ms");
            Environment.Exit(elapsed);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error : {ex.Message}");
            Environment.Exit(-99);
        }
    }
}
