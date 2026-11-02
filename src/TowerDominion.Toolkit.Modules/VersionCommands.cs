using TowerDominion.Toolkit.Model;

namespace TowerDominion.Toolkit.Modules;

/// <summary>Reports compiled build identity and the actual registered command/module manifest.</summary>
public sealed class VersionCommands
{
    private readonly CommandRegistry commands;
    private readonly ModuleRegistry modules;
    private readonly CapabilityRegistry capabilities;

    public VersionCommands(CommandRegistry commands, ModuleRegistry modules, CapabilityRegistry capabilities)
    {
        this.commands = commands;
        this.modules = modules;
        this.capabilities = capabilities;
    }

    public string Execute(string input)
    {
        if (!input.Trim().Equals("version", StringComparison.OrdinalIgnoreCase))
            return "Usage: version";

        string[] commandNames = commands.Commands.Select(command => command.Name).ToArray();
        string[] moduleIds = modules.GetAll().Select(module => module.Id)
            .OrderBy(id => id, StringComparer.OrdinalIgnoreCase).ToArray();
        string[] capabilityNames = capabilities.GetAll().Select(capability => capability.Id)
            .OrderBy(id => id, StringComparer.OrdinalIgnoreCase).ToArray();

        return $"{ToolkitBuildInfo.Name} v{ToolkitBuildInfo.Version}" + Environment.NewLine
            + $"Build commit: {ToolkitBuildInfo.Commit}" + Environment.NewLine
            + $"Commands: {string.Join(", ", commandNames)}" + Environment.NewLine
            + $"Modules: {(moduleIds.Length == 0 ? "none" : string.Join(", ", moduleIds))}" + Environment.NewLine
            + $"Capabilities: {(capabilityNames.Length == 0 ? "none registered" : string.Join(", ", capabilityNames))}";
    }
}
