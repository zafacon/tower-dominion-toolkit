using TowerDominion.Toolkit.Model;

namespace TowerDominion.Toolkit.Modules;

public sealed class ChallengePresetAdapter
{
    private readonly ChallengeSelections selections;
    private readonly CommandPresetStore<string[]> store;

    public ChallengePresetAdapter(string directory, ChallengeRegistry registry, ChallengeSelections selections)
    {
        ArgumentNullException.ThrowIfNull(registry);
        this.selections = selections ?? throw new ArgumentNullException(nameof(selections));
        store = new(directory, ids => ids.Any(id => string.IsNullOrWhiteSpace(id) || !registry.TryGet(id, out _))
            ? "Challenge preset contains an unknown identifier." : null);
    }

    public IReadOnlyList<string> Names() => store.Names();
    public string[] Snapshot() => selections.Selected.ToArray();
    public void Save(string name) => store.Save(name, selections.Selected.ToArray());
    public IReadOnlyList<string> Inspect(string name) => store.Read(name);
    public void Load(string name)
    {
        if (selections.RunActive) throw new InvalidOperationException("Challenges cannot be changed during a run.");
        selections.Replace(store.Read(name));
    }
    public bool Delete(string name) => store.Delete(name);
}
