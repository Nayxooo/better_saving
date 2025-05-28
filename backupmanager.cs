using System.Threading;
using System.Threading.Tasks;

public class BackupManager
{
    private SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1); // Un seul accès à la fois

    // Méthode asynchrone qui sera appelée dans ton SaveAsync()
    public async Task<bool> SaveAsync()
    {
        await _semaphore.WaitAsync();

        try
        {
            if (IsNetworkLoadHigh())
            {
                // Charge réseau trop élevée, on refuse ou reporte
                return false;
            }

            // Lancer la sauvegarde (simulée ici)
            await PerformBackupAsync();
            return true;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private bool IsNetworkLoadHigh()
    {
        // TODO : Implémenter ta détection de charge réseau réelle
        return false; // exemple par défaut
    }

    private async Task PerformBackupAsync()
    {
        // Simule une sauvegarde qui dure 5 secondes
        await Task.Delay(5000);
    }
}
