using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;

namespace better_saving.Models
{
    /// <summary>
    /// Handles downloading CryptoSoft.exe from the official repository
    /// </summary>
    public static class CryptoSoftDownloader
    {
        private const string CRYPTOSOFT_DOWNLOAD_URL = "https://github.com/Nayxooo/better_saving/releases/download/v1.1.0/CryptoSoft.exe";
        private static readonly string CryptoSoftExePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CryptoSoft.exe");

        /// <summary>
        /// Downloads CryptoSoft.exe from the official repository
        /// </summary>
        /// <returns>True if download was successful, false otherwise</returns>
        public static async Task<bool> DownloadCryptoSoftAsync()
        {
            try
            {
                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromMinutes(5); // Set a reasonable timeout

                // Download the file
                var response = await httpClient.GetAsync(CRYPTOSOFT_DOWNLOAD_URL);

                if (!response.IsSuccessStatusCode)
                {
                    ShowErrorMessage($"Failed to download CryptoSoft.exe. HTTP Status: {response.StatusCode}");
                    return false;
                }

                var fileBytes = await response.Content.ReadAsByteArrayAsync();

                // Write the file to disk
                await File.WriteAllBytesAsync(CryptoSoftExePath, fileBytes);

                // Verify the file was written correctly
                if (File.Exists(CryptoSoftExePath))
                {
                    ShowSuccessMessage("CryptoSoft.exe downloaded successfully!");
                    return true;
                }
                else
                {
                    ShowErrorMessage("Failed to save CryptoSoft.exe to disk.");
                    return false;
                }
            }
            catch (HttpRequestException ex)
            {
                ShowErrorMessage($"Network error while downloading CryptoSoft.exe: {ex.Message}");
                return false;
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                ShowErrorMessage("Download timed out. Please check your internet connection and try again.");
                return false;
            }
            catch (UnauthorizedAccessException ex)
            {
                ShowErrorMessage($"Access denied while saving CryptoSoft.exe: {ex.Message}");
                return false;
            }
            catch (DirectoryNotFoundException ex)
            {
                ShowErrorMessage($"Directory not found: {ex.Message}");
                return false;
            }
            catch (IOException ex)
            {
                ShowErrorMessage($"IO error while downloading CryptoSoft.exe: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"Unexpected error while downloading CryptoSoft.exe: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Downloads CryptoSoft.exe synchronously (blocks the calling thread)
        /// </summary>
        /// <returns>True if download was successful, false otherwise</returns>
        public static bool DownloadCryptoSoft()
        {
            try
            {
                return DownloadCryptoSoftAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"Error during synchronous download: {ex.Message}");
                return false;
            }
        }

        private static void ShowErrorMessage(string message)
        {
            try
            {
                System.Windows.MessageBox.Show(
                    message,
                    "CryptoSoft Download Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch
            {
                // If we can't show a message box, write to console as fallback
                Console.Error.WriteLine($"CryptoSoft Download Error: {message}");
            }
        }

        private static void ShowSuccessMessage(string message)
        {
            try
            {
                System.Windows.MessageBox.Show(
                    message,
                    "CryptoSoft Download",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch
            {
                // If we can't show a message box, write to console as fallback
                Console.WriteLine($"CryptoSoft Download: {message}");
            }
        }
    }
}
