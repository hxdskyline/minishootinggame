using System.IO;
using UnityEngine;

public static class FileLogger
{
    private static readonly string logPath = Path.Combine(Application.persistentDataPath, "game_log.txt");

    public static void Log(string message)
    {
        try
        {
            string line = System.DateTime.Now.ToString("HH:mm:ss.fff") + " | " + message + "\n";
            File.AppendAllText(logPath, line);
        }
        catch { }
    }

    public static void Clear()
    {
        try { File.WriteAllText(logPath, ""); } catch { }
    }
}
