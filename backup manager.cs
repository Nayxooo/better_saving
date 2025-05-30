using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using BetterSaving.Infrastructure;


namespace BetterSaving.Infrastructure
{
    public class NetworkBandwidthMonitor
    {
        private string? GetActiveNetworkInterfaceName()
        {
            var category = new PerformanceCounterCategory("Network Interface");
            var instanceNames = category.GetInstanceNames();
            return instanceNames.FirstOrDefault(name => !name.ToLower().Contains("loopback"));
        }

        public (double downKbps, double upKbps) Sample()
        {
            string? name = GetActiveNetworkInterfaceName();
            if (name == null)
                return (0, 0);

            var download = new PerformanceCounter("Network Interface", "Bytes Received/sec", name);
            var upload = new PerformanceCounter("Network Interface", "Bytes Sent/sec", name);

            _ = download.NextValue();
            _ = upload.NextValue();

            Thread.Sleep(1000);

            float down = download.NextValue();
            float up = upload.NextValue();

            return ((down * 8) / 1024, (up * 8) / 1024); // en kbps
        }
    }
}

namespace BetterSaving.Models
{
    public class BackupManager
    {
        private readonly NetworkBandwidthMonitor _monitor;
        private readonly int _maxParallel;
        private readonly SemaphoreSlim _semaphore;
        private readonly ConcurrentQueue<Func<CancellationToken, Task>> _queue = new();

        public BackupManager(NetworkBandwidthMonitor monitor, int maxParallel = 5)
        {
            _monitor = monitor;
            _maxParallel = maxParallel;
            _semaphore = new SemaphoreSlim(maxParallel);
        }

        public void Enqueue(Func<CancellationToken, Task> work) => _queue.Enqueue(work);

        public async Task RunAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                if (_queue.TryDequeue(out var job))
                {
                    await _semaphore.WaitAsync(ct);

                    _ = Task.Run(async () =>
                    {
                        try { await job(ct); }
                        finally { _semaphore.Release(); }
                    }, ct);
                }

                var (down, up) = _monitor.Sample();
                int allowed = AdjustParallel(down + up);
                int current = _semaphore.CurrentCount;

                // Ajustement de la capacité du sémaphore
                if (current > allowed)
                {
                    int delta = current - allowed;
                    for (int i = 0; i < delta; i++) await _semaphore.WaitAsync(ct);
                }
                else if (current < allowed)
                {
                    int delta = allowed - current;
                    for (int i = 0; i < delta; i++) _semaphore.Release();
                }

                await Task.Delay(1000, ct);
            }
        }

        private int AdjustParallel(double kbps)
        {
            if (kbps > 5000) return 1;
            if (kbps > 3000) return 2;
            if (kbps > 1500) return 3;
            return _maxParallel;
        }
    }
}
