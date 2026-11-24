using TowerDominion.Toolkit.Rng;
using UnityEngine;

namespace TowerDominion.Toolkit.Game;

internal static class CorrectiveRngGamePersistencePaths
{
    internal static CorrectiveRngPersistencePaths Resolve() =>
        CorrectiveRngPersistencePaths.BesideNativeSave(
            Path.Combine(Application.persistentDataPath, "SaveFolder", "Progress.gd"));
}
