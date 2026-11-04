using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Tests;

public sealed class CommandPresetStoreTests
{
    private static CommandPresetStore<string[]> Create(string directory) =>
        new(directory, values => values.Length == 0 || values.Any(string.IsNullOrWhiteSpace) ? "Invalid snapshot." : null);

    [Fact]
    public void SaveReadListDeleteRoundTrip()
    {
        string path = Path.Combine(Path.GetTempPath(), "td-preset-" + Guid.NewGuid());
        try
        {
            var store = Create(path);
            Assert.Empty(store.Names());
            store.Save("My Setup", ["Alpha", "Beta"]);
            Assert.Equal(["My Setup"], store.Names());
            Assert.Equal(["Alpha", "Beta"], store.Read("My Setup"));
            Assert.True(store.Delete("My Setup"));
            Assert.False(store.Delete("My Setup"));
        }
        finally { if (Directory.Exists(path)) Directory.Delete(path, true); }
    }

    [Theory]
    [InlineData("../outside")]
    [InlineData("a/b")]
    [InlineData("..")]
    [InlineData(" ")]
    public void InvalidNamesCannotEscapeStore(string name)
    {
        var store = Create(Path.Combine(Path.GetTempPath(), "td-preset-" + Guid.NewGuid()));
        Assert.Throws<ArgumentException>(() => store.Save(name, ["Alpha"]));
        Assert.Throws<ArgumentException>(() => store.Read(name));
        Assert.Throws<ArgumentException>(() => store.Delete(name));
    }

    [Fact]
    public void InvalidSnapshotDoesNotOverwriteValidSavedData()
    {
        string path = Path.Combine(Path.GetTempPath(), "td-preset-" + Guid.NewGuid());
        try
        {
            var store = Create(path);
            store.Save("test", ["valid"]);
            Assert.Throws<ArgumentException>(() => store.Save("test", [""]));
            Assert.Equal(["valid"], store.Read("test"));
        }
        finally { if (Directory.Exists(path)) Directory.Delete(path, true); }
    }

    [Fact]
    public void CorruptPayloadFailsReadWithoutMutatingStore()
    {
        string path = Path.Combine(Path.GetTempPath(), "td-preset-" + Guid.NewGuid());
        try
        {
            var store = Create(path);
            store.Save("test", ["valid"]);
            File.WriteAllText(Path.Combine(path, "test.json"), "[\"\"]");
            Assert.Throws<InvalidDataException>(() => store.Read("test"));
        }
        finally { if (Directory.Exists(path)) Directory.Delete(path, true); }
    }
}
