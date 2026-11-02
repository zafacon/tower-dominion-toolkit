namespace TowerDominion.Toolkit.Modules;

[Flags]
public enum CardinalPaths { None = 0, N = 1, E = 2, S = 4, W = 8 }
public enum StartPosition { N, E, S, W }
public enum StartShape { L, S, T, X }

public sealed record StartingTile(StartPosition Position, StartShape Shape, CardinalPaths Paths, int Rotation);

public static class StartingTiles
{
    private static readonly CardinalPaths BaseL = CardinalPaths.N | CardinalPaths.W;
    private static readonly CardinalPaths BaseS = CardinalPaths.N | CardinalPaths.S;
    private static readonly CardinalPaths BaseT = CardinalPaths.N | CardinalPaths.E | CardinalPaths.W;
    private static readonly CardinalPaths BaseX = CardinalPaths.N | CardinalPaths.E | CardinalPaths.S | CardinalPaths.W;

    public static StartingTile Parse(string text)
    {
        var parts = text.Trim().Split(':', 2);
        if (parts.Length != 2 || !Enum.TryParse<StartPosition>(parts[0], true, out var position))
            throw new ArgumentException("Starting tile must begin with N:, E:, S:, or W:.");

        var shapeParts = parts[1].Split('.', 2);
        if (!Enum.TryParse<StartShape>(shapeParts[0], true, out var shape))
            throw new ArgumentException("Starting tile shape must be L, S, T, or X.");

        char? orientation = null;
        if (shapeParts.Length == 2)
        {
            var value = shapeParts[1].Trim().ToUpperInvariant();
            if (value is not ("C" or "A"))
                throw new ArgumentException("Orientation suffix must be C (clockwise) or A (anticlockwise).");
            orientation = value[0];
        }

        var hq = HqFacing(position);
        var clockwise = TurnClockwise(hq);
        var anticlockwise = TurnAnticlockwise(hq);
        CardinalPaths wanted;
        switch (shape)
        {
            case StartShape.L:
                if (orientation == null) throw new ArgumentException("L requires .C or .A.");
                wanted = hq | (orientation == 'C' ? clockwise : anticlockwise);
                break;
            case StartShape.S:
                if (orientation != null) throw new ArgumentException("S does not accept an orientation suffix.");
                wanted = hq | Opposite(hq);
                break;
            case StartShape.T:
                wanted = orientation switch
                {
                    null => hq | clockwise | anticlockwise,
                    // T.C/T.A are viewed while travelling outward from the HQ.
                    // That reverses the lateral sense relative to looking toward HQ.
                    'C' => hq | anticlockwise | Opposite(hq),
                    'A' => hq | clockwise | Opposite(hq),
                    _ => throw new ArgumentOutOfRangeException()
                };
                break;
            case StartShape.X:
                if (orientation != null) throw new ArgumentException("X does not accept an orientation suffix.");
                wanted = BaseX;
                break;
            default: throw new ArgumentOutOfRangeException();
        }
        return new StartingTile(position, shape, wanted, FindRotation(shape, wanted));
    }

    public static IReadOnlyDictionary<StartPosition, StartingTile> ParseSet(string text)
    {
        var result = new Dictionary<StartPosition, StartingTile>();
        foreach (var token in text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var tile = Parse(token);
            if (!result.TryAdd(tile.Position, tile))
                throw new ArgumentException("Starting position " + tile.Position + " was specified more than once.");
        }
        if (result.Count != 4)
            throw new ArgumentException("Starting tiles require exactly N, E, S, and W.");
        return result;
    }

    public static CardinalPaths Rotate(CardinalPaths paths, int degrees)
    {
        var steps = ((degrees % 360) + 360) % 360 / 90;
        for (var i = 0; i < steps; i++)
        {
            var next = CardinalPaths.None;
            if ((paths & CardinalPaths.N) != 0) next |= CardinalPaths.W;
            if ((paths & CardinalPaths.W) != 0) next |= CardinalPaths.S;
            if ((paths & CardinalPaths.S) != 0) next |= CardinalPaths.E;
            if ((paths & CardinalPaths.E) != 0) next |= CardinalPaths.N;
            paths = next;
        }
        return paths;
    }

    public static int FindRotation(StartShape shape, CardinalPaths wanted)
    {
        var basis = shape switch
        {
            StartShape.L => BaseL,
            StartShape.S => BaseS,
            StartShape.T => BaseT,
            StartShape.X => BaseX,
            _ => throw new ArgumentOutOfRangeException(nameof(shape))
        };
        foreach (var angle in new[] { 0, 90, 180, 270 })
            if (Rotate(basis, angle) == wanted) return angle;
        throw new ArgumentException("Requested path topology cannot be produced by " + shape + ".");
    }

    public static CardinalPaths HqFacing(StartPosition position) => position switch
    {
        StartPosition.N => CardinalPaths.S,
        StartPosition.E => CardinalPaths.W,
        StartPosition.S => CardinalPaths.N,
        StartPosition.W => CardinalPaths.E,
        _ => throw new ArgumentOutOfRangeException(nameof(position))
    };

    public static CardinalPaths TurnClockwise(CardinalPaths p) => p switch
    {
        CardinalPaths.N => CardinalPaths.E,
        CardinalPaths.E => CardinalPaths.S,
        CardinalPaths.S => CardinalPaths.W,
        CardinalPaths.W => CardinalPaths.N,
        _ => throw new ArgumentException("Direction must be cardinal.")
    };

    public static CardinalPaths TurnAnticlockwise(CardinalPaths p) => p switch
    {
        CardinalPaths.N => CardinalPaths.W,
        CardinalPaths.W => CardinalPaths.S,
        CardinalPaths.S => CardinalPaths.E,
        CardinalPaths.E => CardinalPaths.N,
        _ => throw new ArgumentException("Direction must be cardinal.")
    };

    private static CardinalPaths Opposite(CardinalPaths p) => p switch
    {
        CardinalPaths.N => CardinalPaths.S,
        CardinalPaths.E => CardinalPaths.W,
        CardinalPaths.S => CardinalPaths.N,
        CardinalPaths.W => CardinalPaths.E,
        _ => throw new ArgumentException("Direction must be cardinal.")
    };
    private static bool IsSingle(CardinalPaths p) => p is CardinalPaths.N or CardinalPaths.E or CardinalPaths.S or CardinalPaths.W;
}
