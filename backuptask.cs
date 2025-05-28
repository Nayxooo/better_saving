using System;
using System.IO;
using System.Threading.Tasks;

public class BackupTask
{
    private readonly int _id;
    private readonly int _maxKoPerSec;
    private readonly NetworkMonitor _networkMonitor;
    private readonly CriticalProcessDetector _processDetector;

    public BackupTask(int id, int maxKoPerSec, NetworkMonitor netMon, CriticalProcessDetector procDet)
    {
        _id = id;
        _maxKoPerSec = maxKoPerSec;
        _networkMonitor = netMon;
        _processDetector = procDet;
    }

    public async Task RunAsync()
    {
        string file = $"backup_{_id}.bin";
        using (FileStream fs = new FileStream(file, FileMode.Create, FileAccess.Write))
        {
            byte[] buffer = new byte[1024]; // 1 Ko
            Random rnd = new Random();
            for (int i = 0; i < 5000; i++) // 5 Mo
            {
                rnd.NextBytes(buffer);
                await fs.WriteAsync(buffer, 0, buffer.Length);
                fs.Flush();

                // Régulation
                float currentBandwidth = _networkMonitor.GetCurrentBandwidthKoPerSec();
                bool criticalRunning = _processDetector.IsAnyCriticalProcessRunning();

                if (criticalRunning)
                    await Task.Delay(100); // Forte régulation
                else if (currentBandwidth > _maxKoPerSec * 1.5)
                    await Task.Delay(50); // Régulation modérée
            }
        }
    }
}
