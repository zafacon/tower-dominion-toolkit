using TowerDominion.Toolkit.Model;
using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Tests;

public sealed class ChallengePresetAdapterTests
{
    private sealed class Rule(string id) : IChallengeRule
    {
        public ChallengeDescriptor Descriptor { get; } = new(id, id, "Test", new HashSet<ChallengeActionKind>());
        public ChallengeDecision Evaluate(ChallengeAction action) => ChallengeDecision.Permit();
    }

    [Fact]
    public void NamedChallengePresetReplacesSelectionAndPersists()
    {
        string root = Path.Combine(Path.GetTempPath(), "td-chpreset-" + Guid.NewGuid());
        try
        {
            var registry = new ChallengeRegistry();
            registry.Register(new Rule("one"));
            registry.Register(new Rule("two"));
            string path = Path.Combine(root, "selected.json");
            var selections = new ChallengeSelections(path, registry);
            var adapter = new ChallengePresetAdapter(Path.Combine(root, "presets"), registry, selections);
            selections.Change(["one"], true);
            adapter.Save("Original");
            selections.Change(["two"], true);
            adapter.Load("Original");
            Assert.Equal(["one"], selections.Selected);
            Assert.Equal(["one"], new ChallengeSelections(path, registry).Selected);
            Assert.Equal(["one"], adapter.Inspect("Original"));
            Assert.Equal(["Original"], adapter.Names());
            Assert.True(adapter.Delete("Original"));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public void ActiveRunPreventsPresetLoadAndPreservesRunSnapshot()
    {
        string root = Path.Combine(Path.GetTempPath(), "td-chpreset-" + Guid.NewGuid());
        try
        {
            var registry = new ChallengeRegistry();
            registry.Register(new Rule("one"));
            registry.Register(new Rule("two"));
            var selections = new ChallengeSelections(Path.Combine(root, "selected.json"), registry);
            var adapter = new ChallengePresetAdapter(Path.Combine(root, "presets"), registry, selections);
            selections.Change(["one"], true);
            adapter.Save("One");
            selections.Change(["two"], true);
            selections.StartRun();
            Assert.Throws<InvalidOperationException>(() => adapter.Load("One"));
            Assert.Equal(["one", "two"], selections.Selected);
            Assert.Equal(["one", "two"], selections.Active);
            selections.EndRun();
            adapter.Load("One");
            Assert.Equal(["one"], selections.Selected);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public void InvalidPresetDoesNotPartiallyReplaceSelection()
    {
        string root = Path.Combine(Path.GetTempPath(), "td-chpreset-" + Guid.NewGuid());
        try
        {
            var registry = new ChallengeRegistry();
            registry.Register(new Rule("one"));
            var selections = new ChallengeSelections(Path.Combine(root, "selected.json"), registry);
            var adapter = new ChallengePresetAdapter(Path.Combine(root, "presets"), registry, selections);
            selections.Change(["one"], true);
            adapter.Save("One");
            File.WriteAllText(Path.Combine(root, "presets", "One.json"), "[\"one\",\"missing\"]");
            Assert.Throws<InvalidDataException>(() => adapter.Load("One"));
            Assert.Equal(["one"], selections.Selected);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
}
