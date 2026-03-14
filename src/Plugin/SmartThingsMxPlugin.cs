using Loupedeck;

namespace SmartThingsMxConsole.Plugin;

public sealed class SmartThingsMxConsolePlugin : Loupedeck.Plugin
{
    public SmartThingsMxConsolePlugin()
    {
        PluginDiagnostics.RegisterExceptionTracing();
        PluginDiagnostics.Write("SmartThingsMxConsolePlugin constructor");
    }

    public override bool UsesApplicationApiOnly => true;

    public override bool HasNoApplication => true;

    internal void UpdatePluginStatus(Loupedeck.PluginStatus status, string message) =>
        OnPluginStatusChanged(status, message);

    public override void Load()
    {
        PluginDiagnostics.Write("Load start");
        try
        {
            PluginDiagnostics.Write("Load complete");
        }
        catch (Exception exception)
        {
            PluginDiagnostics.Write($"Load failed: {exception}");
            throw;
        }
    }

    public override void Unload()
    {
        PluginDiagnostics.Write("Unload start");
        try
        {
            PluginDiagnostics.Write("Unload complete");
        }
        catch (Exception exception)
        {
            PluginDiagnostics.Write($"Unload failed: {exception}");
            throw;
        }
    }
}
