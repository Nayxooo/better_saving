using System.Net.NetworkInformation;

namespace Better_saving.Models
{
    public class NetworkBandwidthMonitor
    {
        private long _lastRx, _lastTx;
        private readonly NetworkInterface _nic;

        public NetworkBandwidthMonitor(string nicName)
        {
            _nic = NetworkInterface.GetAllNetworkInterfaces()
                                   .FirstOrDefault(n => n.Name == nicName)
                   ?? throw new ArgumentException($"Interface {nicName} introuvable");
            var stats = _nic.GetIPv4Statistics();
            _lastRx = stats.BytesReceived;
            _lastTx = stats.BytesSent;
        }

        public (double downKbps, double upKbps) Sample()
        {
            var s = _nic.GetIPv4Statistics();
            var rx = s.BytesReceived;
            var tx = s.BytesSent;

            double down = (rx - _lastRx) / 1024.0;
            double up = (tx - _lastTx) / 1024.0;

            _lastRx = rx;
            _lastTx = tx;

            return (down, up);
        }
    }
}
