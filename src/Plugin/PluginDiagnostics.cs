namespace SmartThingsMxConsole.Plugin;

internal static class PluginDiagnostics
{
    private static int _exceptionTracingRegistered;
    private static readonly string LogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "SmartThingsMxConsole",
        "plugin-debug.log");

    public static void Write(string message)
    {
        try
        {
            var directory = Path.GetDirectoryName(LogPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.AppendAllText(LogPath, $"{DateTimeOffset.UtcNow:O} | {message}{Environment.NewLine}");
        }
        catch
        {
        }
    }

    public static void RegisterExceptionTracing()
    {
        if (Interlocked.Exchange(ref _exceptionTracingRegistered, 1) == 1)
        {
            return;
        }

        try
        {
            AppDomain.CurrentDomain.FirstChanceException += (_, eventArgs) =>
            {
                try
                {
                    Write($"FirstChanceException: {eventArgs.Exception.GetType().FullName}: {eventArgs.Exception.Message}");
                }
                catch
                {
                }
            };

            AppDomain.CurrentDomain.UnhandledException += (_, eventArgs) =>
            {
                try
                {
                    Write($"UnhandledException: {eventArgs.ExceptionObject}");
                }
                catch
                {
                }
            };

            Write("Exception tracing registered");
        }
        catch
        {
        }
    }
}
