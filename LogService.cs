namespace iRacingWheelSync;

public static class LogService
{
    private static readonly string LogPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "iRacingWheelSyncLog.log");

    public static void AddLog(string log)
    {
        var time = DateTime.Now;

        File.AppendAllText(LogPath, $"[ {time:HH:mm:ss} ] {log}\n");
    }


    public static void CheckLogFile()
    {
        if (!File.Exists(LogPath))
        {
            File.Create(LogPath).Dispose();
        }
    }
}