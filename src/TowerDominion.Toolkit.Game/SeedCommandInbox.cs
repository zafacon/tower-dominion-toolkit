using MelonLoader;
using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Game;

/// <summary>Minimal file-based command transport for isolated testing, polled on the game thread.</summary>
internal sealed class SeedCommandInbox
{
    private readonly string inboxPath;
    private readonly string responsePath;
    private readonly CommandRegistry commands;

    public SeedCommandInbox(string directory, CommandRegistry commands)
    {
        ArgumentNullException.ThrowIfNull(commands);
        Directory.CreateDirectory(directory);
        inboxPath = Path.Combine(directory, "seed-command.txt");
        responsePath = Path.Combine(directory, "seed-response.txt");
        this.commands = commands;
    }

    public void Poll()
    {
        if (!File.Exists(inboxPath))
        {
            return;
        }

        try
        {
            string command = File.ReadAllText(inboxPath).Trim();
            File.Delete(inboxPath);
            string response = commands.Execute(command);
            File.WriteAllText(responsePath, response + Environment.NewLine);
            MelonLogger.Msg($"[Toolkit] Seed command: {response}");
        }
        catch (IOException exception)
        {
            MelonLogger.Warning($"[Toolkit] Seed command I/O: {exception.Message}");
        }
        catch (UnauthorizedAccessException exception)
        {
            MelonLogger.Warning($"[Toolkit] Seed command access: {exception.Message}");
        }
    }
}
