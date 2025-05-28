using System.Diagnostics;

public class NetworkMonitor
{
    private PerformanceCounter _bandwidthCounter;

    public NetworkMonitor()
    {
        string interfaceName = GetNetworkInterface();
        _bandwidthCounter = new PerformanceCounter("Network Interface", "Bytes Total/sec", interfaceName);
    }

    public float GetCurrentBandwidthKoPerSec()
    {
        float bytesPerSec = _bandwidthCounter.NextValue();
        return bytesPerSec / 1024f;
    }

    private string GetNetworkInterface()
    {
        var cat = new PerformanceCounterCategory("Network Interface");
        string[] instances = cat.GetInstanceNames();
        return instances.Length > 0 ? instances[0] : throw new Exception("No network interface found.");
    }
}
