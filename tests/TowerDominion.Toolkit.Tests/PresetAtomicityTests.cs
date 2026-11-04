using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Tests;

public sealed class PresetAtomicityTests
{
    [Fact]
    public void LatePresetCollisionLeavesRegistryUnchanged()
    {
        var grammar = new CommandGrammar();
        grammar.RegisterDomain(new("frontiers", "Modifiers"));
        grammar.Register(new("delete", "frontiers", "Delete by name",
            [new ArgumentDefinition("name", "Name", Completion: ArgumentCompletion.FreeForm)]));
        var original = grammar.Operations.ToArray();
        var handlers = new PresetHandlers(
            _ => CommandOutcome.Ok("save"), _ => CommandOutcome.Ok("load"),
            _ => CommandOutcome.Ok("list"), _ => CommandOutcome.Ok("inspect"),
            _ => CommandOutcome.Ok("delete"));
        Assert.Throws<InvalidOperationException>(() => grammar.RegisterPresets("frontiers", handlers));
        Assert.Equal(original, grammar.Operations);
        Assert.False(grammar.Supports("save", "frontiers", variant: "preset"));
        Assert.False(grammar.Supports("load", "frontiers", variant: "preset"));
    }
}
