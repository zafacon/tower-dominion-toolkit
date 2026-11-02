using TowerDominion.Toolkit.Modules;
using UnityEngine;

namespace TowerDominion.Toolkit.Game;

/// <summary>Read-only, cached, opaque 3x3 starting-tile topology preview.</summary>
internal static class StartingTileMosaic
{
    private const int Size = 192;
    private const int Tile = Size / 3;
    private static readonly Color Ground = new(0.09f, 0.14f, 0.19f, 1f);
    private static readonly Color Road = new(0.86f, 0.91f, 0.96f, 1f);
    private static readonly Color Headquarters = new(0.21f, 0.34f, 0.47f, 1f);
    private static readonly Dictionary<string, Texture2D> Cache = new();
    private static bool textureSupported = true;
    private static bool warningLogged;

    internal static void Draw(Rect area, int difficulty, IReadOnlyList<string> shapes)
    {
        StartPosition[] entrances = difficulty switch
        {
            1 => new[] { StartPosition.N },
            2 => new[] { StartPosition.N, StartPosition.S },
            3 => new[] { StartPosition.N, StartPosition.E, StartPosition.S },
            _ => new[] { StartPosition.N, StartPosition.E, StartPosition.S, StartPosition.W }
        };
        DrawMap(area, RelativeStartingTiles.Map(difficulty, shapes, entrances), "D" + difficulty);
    }

    internal static void DrawMap(Rect area, IReadOnlyDictionary<StartPosition, StartingTile> tiles, string title)
    {
        GUI.Label(new Rect(area.x, area.y, area.width, 20), title);
        float side = Math.Min(area.width / 3, (area.height - 26) / 3);
        float ox = area.x + (area.width - side * 3) / 2;
        float oy = area.y + 24;
        var target = new Rect(ox, oy, side * 3, side * 3);
        if (textureSupported)
        {
            try
            {
                string key = string.Join(";", tiles.OrderBy(t => t.Key).Select(t => t.Key + ":" + (int)t.Value.Paths));
                if (!Cache.TryGetValue(key, out var texture))
                {
                    texture = BuildTexture(tiles);
                    Cache[key] = texture;
                }
                var style = GUI.skin.box;
                var previousTexture = style.normal.background;
                try
                {
                    style.normal.background = texture;
                    GUI.Box(target, "", style);
                }
                finally { style.normal.background = previousTexture; }
                DrawHQLabel(ox + side, oy + side, side);
                return;
            }
            catch (Exception ex) when (ex is NotSupportedException or MissingMethodException or TypeInitializationException)
            {
                textureSupported = false;
                if (!warningLogged)
                {
                    warningLogged = true;
                    MelonLoader.MelonLogger.Warning("[Mosaic] Opaque texture unavailable, using GUI fallback: " + ex);
                }
            }
        }
        DrawFallback(ox, oy, side, tiles);
        DrawHQLabel(ox + side, oy + side, side);
    }

    private static Texture2D BuildTexture(IReadOnlyDictionary<StartPosition, StartingTile> tiles)
    {
        var pixels = new Color[Size * Size];
        Array.Fill(pixels, Ground);
        Paint(pixels, Tile, Tile, Tile, Tile, Headquarters);
        foreach (var entry in tiles)
        {
            var pos = entry.Key;
            int col = pos == StartPosition.W ? 0 : pos == StartPosition.E ? 2 : 1;
            int row = pos == StartPosition.N ? 0 : pos == StartPosition.S ? 2 : 1;
            PaintRoad(pixels, col * Tile, row * Tile, entry.Value.Paths);
        }
        // Subtle grid lines, drawn only where there is no road.
        for (int i = 1; i < 3; i++)
        {
            int edge = i * Tile;
            for (int p = 0; p < Size; p++)
            {
                BorderPixel(pixels, edge, p);
                BorderPixel(pixels, p, edge);
            }
        }
        // Texture pixels have bottom-left origin; flip rows to match GUI coordinates.
        var flipped = new Color[pixels.Length];
        for (int y = 0; y < Size; y++)
            Array.Copy(pixels, y * Size, flipped, (Size - 1 - y) * Size, Size);
        var texture = new Texture2D(Size, Size);
        texture.SetPixels(flipped);
        texture.Apply();
        return texture;
    }

    private static void BorderPixel(Color[] pixels, int x, int y)
    {
        int index = y * Size + x;
        if (pixels[index].r == Ground.r && pixels[index].g == Ground.g)
            pixels[index] = new Color(0.22f, 0.32f, 0.42f, 1f);
    }

    private static void Paint(Color[] pixels, int x, int y, int width, int height, Color color)
    {
        for (int yy = Math.Max(0, y); yy < Math.Min(Size, y + height); yy++)
            for (int xx = Math.Max(0, x); xx < Math.Min(Size, x + width); xx++)
                pixels[yy * Size + xx] = color;
    }

    private static void PaintRoad(Color[] pixels, int x, int y, CardinalPaths paths)
    {
        int w = 12;
        int inset = 3;
        int m = Tile / 2;
        Paint(pixels, x + m - w / 2, y + m - w / 2, w, w, Road);
        if ((paths & CardinalPaths.N) != 0) Paint(pixels, x + m - w / 2, y + inset, w, m - inset, Road);
        if ((paths & CardinalPaths.S) != 0) Paint(pixels, x + m - w / 2, y + m, w, m - inset, Road);
        if ((paths & CardinalPaths.E) != 0) Paint(pixels, x + m, y + m - w / 2, m - inset, w, Road);
        if ((paths & CardinalPaths.W) != 0) Paint(pixels, x + inset, y + m - w / 2, m - inset, w, Road);
    }

    private static void DrawHQLabel(float x, float y, float side) =>
        GUI.Label(new Rect(x + side / 2 - 12, y + side / 2 - 10, 28, 20), "HQ");

    private static void DrawFallback(float ox, float oy, float side, IReadOnlyDictionary<StartPosition, StartingTile> tiles)
    {
        for (int y = 0; y < 3; y++)
            for (int x = 0; x < 3; x++)
                Box(new Rect(ox + x * side, oy + y * side, side, side), Ground);
        Box(new Rect(ox + side, oy + side, side, side), Headquarters);
        foreach (var entry in tiles)
        {
            var pos = entry.Key;
            int col = pos == StartPosition.W ? 0 : pos == StartPosition.E ? 2 : 1;
            int row = pos == StartPosition.N ? 0 : pos == StartPosition.S ? 2 : 1;
            FallbackRoad(ox + col * side, oy + row * side, side, entry.Value.Paths);
        }
    }

    private static void Box(Rect rect, Color color)
    {
        var previous = GUI.backgroundColor;
        try { GUI.backgroundColor = color; GUI.Box(rect, ""); }
        finally { GUI.backgroundColor = previous; }
    }

    private static void FallbackRoad(float x, float y, float side, CardinalPaths paths)
    {
        float w = side * 0.19f, inset = side * 0.047f, m = side / 2;
        Box(new Rect(x + m - w / 2, y + m - w / 2, w, w), Road);
        if ((paths & CardinalPaths.N) != 0) Box(new Rect(x + m - w / 2, y + inset, w, m - inset), Road);
        if ((paths & CardinalPaths.S) != 0) Box(new Rect(x + m - w / 2, y + m, w, m - inset), Road);
        if ((paths & CardinalPaths.E) != 0) Box(new Rect(x + m, y + m - w / 2, m - inset, w), Road);
        if ((paths & CardinalPaths.W) != 0) Box(new Rect(x + inset, y + m - w / 2, m - inset, w), Road);
    }
}
