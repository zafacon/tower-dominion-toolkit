namespace TowerDominion.Toolkit.Rng;

public sealed record CorrectiveRngPersistencePaths(
    string NativeSavePath,
    string SidecarPath,
    string ProvenancePath)
{
    public static CorrectiveRngPersistencePaths BesideNativeSave(string nativeSavePath)
    {
        if (string.IsNullOrWhiteSpace(nativeSavePath))
            throw new ArgumentException("Native save path cannot be null or whitespace.", nameof(nativeSavePath));

        return new(
            nativeSavePath,
            nativeSavePath + ".tdtoolkit-rng.json",
            nativeSavePath + ".tdtoolkit-rng.provenance.json");
    }
}

public interface ICorrectiveRngPersistenceStore
{
    bool Exists(string path);
    byte[] ReadBytes(string path);
    string ReadText(string path);
    void WriteTextAtomic(string path, string content);
    void DeleteIfExists(string path);
}

public sealed class FileCorrectiveRngPersistenceStore : ICorrectiveRngPersistenceStore
{
    public bool Exists(string path) => File.Exists(path);

    public byte[] ReadBytes(string path) => File.ReadAllBytes(path);

    public string ReadText(string path) => File.ReadAllText(path);

    public void WriteTextAtomic(string path, string content)
    {
        string? directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        string temporary = path + ".tmp";
        File.WriteAllText(temporary, content);
        File.Move(temporary, path, overwrite: true);
    }

    public void DeleteIfExists(string path)
    {
        if (File.Exists(path))
            File.Delete(path);
    }
}
