using System.Xml.Linq;

namespace TowerDominion.Toolkit.Tests;

public sealed class ArchitectureBoundaryTests
{
    private static readonly string[] ModelReference = { "TowerDominion.Toolkit.Model" };

    [Fact]
    public void ModelAndModulesHaveOnlyAllowedProjectDependencies()
    {
        string root = FindRepositoryRoot();
        Assert.Empty(GetProjectReferences(root, "Model"));
        Assert.Equal(
            ModelReference,
            GetProjectReferences(root, "Modules"));
        Assert.Equal(
            ModelReference,
            GetProjectReferences(root, "Runtime"));
    }

    [Theory]
    [InlineData("Model")]
    [InlineData("Modules")]
    public void SharedCodeDoesNotReferenceGameRuntimeNamespaces(string project)
    {
        string root = FindRepositoryRoot();
        string directory = Path.Combine(root, "src", "TowerDominion.Toolkit." + project);

        foreach (string file in Directory.EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories)
                     .Where(path => !path.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar)))
        {
            string source = File.ReadAllText(file);
            Assert.DoesNotContain("using MelonLoader", source);
            Assert.DoesNotContain("using HarmonyLib", source);
            Assert.DoesNotContain("using Il2Cpp", source);
            Assert.DoesNotContain("using UnityEngine", source);
        }
    }

    private static string[] GetProjectReferences(string root, string project)
    {
        string path = Path.Combine(root, "src", "TowerDominion.Toolkit." + project,
            "TowerDominion.Toolkit." + project + ".csproj");
        XDocument document = XDocument.Load(path);

        return document.Descendants("ProjectReference")
            .Select(element => Path.GetFileNameWithoutExtension(((string?)element.Attribute("Include") ?? "").Replace('\\', '/')))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "TowerDominion.Toolkit.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find toolkit repository root.");
    }
}
