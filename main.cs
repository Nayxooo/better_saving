// filepath: d:\Cesi\Ripo\Cesi\FISE3\5_génieLogiciel\Projet\Git\better_saving\main.cs
using EasySave.ViewModels; // Required for MainViewModel

class Program
{
    static void Main(string[] args)
    {
        // Initialize MainViewModel with logs directory
        var mainViewModel = new MainViewModel("logs");

        // Initialize ConsoleInterface with the MainViewModel
        ConsoleInterface.Initialize(mainViewModel); // Initialize expects MainViewModel now
        
        // Start the console interface
        ConsoleInterface.Start();
    }
}