using Il2CppNvizzio.Game.UI.Views;
using UnityEngine;

namespace TowerDominion.Toolkit.Game;

/// <summary>Native button anchors for the read-only challenge indicator on setup screens.</summary>
internal static class ChallengePreRunAnchors
{
    internal static RectTransform? CommanderNext(CharacterSelectionView view)
    {
        var button = view?.nextButton;
        if (button == null || !button.gameObject.activeInHierarchy) return null;
        return button.GetComponent<RectTransform>();
    }

    internal static RectTransform? FrontierStart(ExpertModeMenu menu)
    {
        var button = menu?.startButton;
        if (button == null || !button.gameObject.activeInHierarchy) return null;
        return button.GetComponent<RectTransform>();
    }
}
