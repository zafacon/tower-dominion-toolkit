using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Tests;

public sealed class StartingTilePresetAdapterTests
{
    [Fact]
    public void AbsolutePresetRoundTrip()
    {
        string root = Path.Combine(Path.GetTempPath(), "td-tile-adapter-" + Guid.NewGuid());
        try
        {
            var state = new StartingTilePresets(root);
            var adapter = new StartingTilePresetAdapter(state);
            state.Set("N:S,E:T.C,S:L.A,W:X");
            adapter.Save("Absolute");
            state.Clear();
            Assert.Equal(4, adapter.Load("Absolute"));
            Assert.True(state.TryGet(0, 2, out _));
            Assert.Contains("N:S", adapter.Inspect("Absolute"));
            Assert.Equal(["Absolute"], adapter.Names());
            Assert.True(adapter.Delete("Absolute"));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public void RelativePresetRestoresAllDifficulties()
    {
        string root = Path.Combine(Path.GetTempPath(), "td-tile-adapter-" + Guid.NewGuid());
        try
        {
            var state = new StartingTilePresets(root);
            var adapter = new StartingTilePresetAdapter(state);
            state.SetRelative(1, "L.C");
            state.SetRelative(2, "S,T.A");
            adapter.Save("Relative");
            state.ClearRelative(1);
            state.ClearRelative(2);
            Assert.Equal(2, adapter.Load("Relative"));
            Assert.Equal(["L.C"], state.Relative[1]);
            Assert.Equal(["S", "T.A"], state.Relative[2]);
            Assert.Contains("D2:", adapter.Inspect("Relative"));
            Assert.Equal(2, new StartingTilePresets(root).Relative.Count);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public void InvalidLaterDifficultyDoesNotPartiallyApplyRelativePreset()
    {
        string root = Path.Combine(Path.GetTempPath(), "td-tile-adapter-" + Guid.NewGuid());
        try
        {
            var state = new StartingTilePresets(root);
            var adapter = new StartingTilePresetAdapter(state);
            state.SetRelative(1, "S");
            adapter.Save("Relative");
            string savedPath = Path.Combine(root, "Relative.json");
            File.WriteAllText(savedPath, "{\"relative\":{\"1\":[\"L.C\"],\"3\":[\"S\"]}}");
            Assert.Throws<ArgumentException>(() => adapter.Load("Relative"));
            Assert.Equal(["S"], state.Relative[1]);
            Assert.Single(new StartingTilePresets(root).Relative);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
}
