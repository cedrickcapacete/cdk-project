using System;
using System.IO;

namespace HarborKiosk.App.Services;

public static class ErrorLog
{
    private static readonly object Gate = new();
    private static string? _path;

    public static void Initialize(string directory)
    {
        Directory.CreateDirectory(directory);
        _path = Path.Combine(directory, "errors.log");
    }

    public static void Write(Exception? exception)
    {
        if (exception is null || _path is null)
        {
            return;
        }

        lock (Gate)
        {
            try
            {
                File.AppendAllText(_path, $"[{DateTimeOffset.Now:O}] {exception}{Environment.NewLine}{Environment.NewLine}");
            }
            catch (IOException)
            {
            }
        }
    }
}
