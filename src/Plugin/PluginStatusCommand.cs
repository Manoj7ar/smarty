using Loupedeck;

namespace SmartThingsMxConsole.Plugin;

public sealed class PluginStatusCommand : PluginDynamicCommand
{
    public PluginStatusCommand()
        : base("SmartThings Status", "Show SmartThings plugin health.", "System", DeviceType.LoupedeckCtFamily)
    {
        PluginDiagnostics.Write("Constructing PluginStatusCommand");
        PluginDiagnostics.Write("Constructed PluginStatusCommand");
    }

    protected override void RunCommand(string actionParameter)
    {
    }

    protected override string GetCommandDisplayName(string actionParameter, PluginImageSize imageSize) =>
        "SmartThings Ready";
}
