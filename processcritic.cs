using System.Diagnostics;

public class CriticalProcessDetector
{
    private readonly string[] _criticalProcesses = new[]
    {
        "ffmpeg", "obs64", "WindowsUpdate", "render", "davinciresolve", "adobe", "vlc"
    };

    public bool IsAnyCriticalProcessRunning()
    {
        foreach (var proc in Process.GetProcesses())
        {
            try
            {
                string name = proc.ProcessName.ToLower();
                foreach (string critical in _criticalProcesses)
                {
                    if (name.Contains(critical.ToLower()))
                        return true;
                }
            }
            catch { /* Ignore inaccessible processes */ }
        }
        return false;
    }
}
