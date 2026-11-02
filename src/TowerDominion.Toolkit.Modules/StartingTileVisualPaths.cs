namespace TowerDominion.Toolkit.Modules;

/// <summary>Visual road topology matching the native zone orientation boundary.</summary>
public static class StartingTileVisualPaths
{
    /// <summary>HQ-outward topology for the schematic, independent of native placement rotations.</summary>
    public static CardinalPaths ForPreview(StartingTile tile)
    {
        if (tile.Shape != StartShape.L) return tile.Paths;
        // The legacy L parser uses HQ-inward handedness; reverse its lateral
        // branch for a diagram viewed while travelling outward from HQ.
        CardinalPaths inward = StartingTiles.HqFacing(tile.Position);
        CardinalPaths clockwise = StartingTiles.TurnClockwise(inward);
        CardinalPaths anticlockwise = StartingTiles.TurnAnticlockwise(inward);
        return inward | ((tile.Paths & clockwise) != 0 ? anticlockwise : clockwise);
    }

    public static CardinalPaths ForNativeZone(StartingTile tile)
    {
        CardinalPaths desired = tile.Paths;
        if (tile.Shape == StartShape.T)
        {
            CardinalPaths towardHq = StartingTiles.HqFacing(tile.Position);
            CardinalPaths outward = StartingTiles.Rotate(towardHq, 180);
            if ((desired & outward) != 0)
            {
                CardinalPaths clockwise = StartingTiles.TurnClockwise(towardHq);
                CardinalPaths anticlockwise = StartingTiles.TurnAnticlockwise(towardHq);
                desired = towardHq | outward |
                    ((desired & clockwise) != 0 ? anticlockwise : clockwise);
            }
        }
        return tile.Position is StartPosition.N or StartPosition.S
            ? StartingTiles.Rotate(desired, 180)
            : desired;
    }
}
