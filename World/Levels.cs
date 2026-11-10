using EchoesOfThePast.Art;
using Microsoft.Xna.Framework;

namespace EchoesOfThePast.World;

/// <summary>
/// Level layouts, in play order. '#' wall, '.' floor, 'B' Ben's spawn, 'P' round button (characters) and 'Q' square plate (crates), 'E' crate switch (stays on) (a level may
/// have several; the door opens only while all are pressed), 'D' door, 'X' exit, 'C' crate, 'G' or
/// ^ v < > a fixed guard (facing). 'T' is a wall torch, 'O' a stone pillar, 'r' a faint floor rune, and ',' '=' '~' are floor decorations (debris, vent, carpet). Patrolling sentries are not drawn in the layout; they come from
/// <see cref="Patrols"/>. Each level needs a 'B', at least one 'P', a 'D' and an 'X'.
/// </summary>
public static class Levels
{
    /// <summary>Only Chapter 1 is playable for now; chapters 2 and 3 are switched off.</summary>
    public const int PerEpisode = 5;
    public const int Episodes = 1;
    public const int Count = PerEpisode * Episodes;

    /// <summary>The art theme of a level (0-based): entrance, twin hall, guard corridor, storeroom, boss arena.</summary>
    public static int Theme(int index) => System.Math.Clamp(index, 0, 4);

    /// <summary>Every room is the size of the first, so the menus and the view are sized to it (20x15 tiles = 320x240 px).</summary>
    public static Point WorldPixelSize
    {
        get
        {
            TileMap first = TileMap.FromStrings(All[0]);
            return new Point(first.PixelWidth, first.PixelHeight);
        }
    }

    /// <summary>On-screen tutorial hints for a level: the next one shows once Ben has taken this many steps.</summary>
    public static (int FromStep, string Text)[] Hints(int index) =>
        index == 0
            ? new[]
            {
                (0, "USE ARROW KEYS / WASD TO MOVE"),
                (3, "REACH THE GREEN EXIT TILE"),
                (10, "PRESS R TO RESTART IF YOU GET STUCK"),
            }
            : System.Array.Empty<(int, string)>();

    /// <summary>The layout for a level number (0-based).</summary>
    public static string[] Layout(int index) => All[index];

    /// <summary>
    /// Turn rules for a level's fixed guards, in reading order (top to bottom, left to right) of the
    /// guards in its layout: how many of Ben's steps between turns (0 = never) and clockwise quarter
    /// turns each time (1 = 90 degrees, 2 = 180, 3 = 90 counter-clockwise). Guards without a rule stay fixed.
    /// </summary>
    public static (int Every, int Quarters)[] GuardRules(int index) =>
        GuardRuleTable.TryGetValue(index, out var rules) ? rules : System.Array.Empty<(int, int)>();

    private static readonly System.Collections.Generic.Dictionary<int, (int Every, int Quarters)[]> GuardRuleTable = new();

    /// <summary>
    /// The sentries that patrol in a level (Guards and the Kidnapper). Each walks through its waypoints in
    /// order, in straight legs, one tile every MoveEvery of Ben's steps, looking the way it walks. A looping
    /// route circles back to its first waypoint; otherwise the sentry walks to the end and back.
    /// </summary>
    public static (CharacterKind Kind, Point[] Route, int MoveEvery, bool Loop)[] Patrols(int index)
    {
        if (!PatrolTable.TryGetValue(index, out var patrols))
            return System.Array.Empty<(CharacterKind, Point[], int, bool)>();

        var result = new (CharacterKind, Point[], int, bool)[patrols.Length];
        for (int n = 0; n < patrols.Length; n++)
        {
            var p = patrols[n];
            result[n] = (p.Kind == 'K' ? CharacterKind.Kidnapper : CharacterKind.Guard, ExpandRoute(p.Waypoints, p.Loop), p.MoveEvery, p.Loop);
        }

        return result;
    }

    /// <summary>Every tile along the straight legs between waypoints (no repeats; a loop's closing leg is included).</summary>
    private static Point[] ExpandRoute(Point[] waypoints, bool loop)
    {
        var route = new System.Collections.Generic.List<Point> { waypoints[0] };
        int legs = loop ? waypoints.Length : waypoints.Length - 1;
        for (int i = 0; i < legs; i++)
        {
            Point from = waypoints[i], to = waypoints[(i + 1) % waypoints.Length];
            int dx = System.Math.Sign(to.X - from.X), dy = System.Math.Sign(to.Y - from.Y);
            int length = System.Math.Max(System.Math.Abs(to.X - from.X), System.Math.Abs(to.Y - from.Y));
            for (int step = 1; step <= length; step++)
                route.Add(new Point(from.X + dx * step, from.Y + dy * step));
        }

        if (loop)
            route.RemoveAt(route.Count - 1); // the closing leg ends on the first tile again

        return route.ToArray();
    }

    private static Point[] Way(params (int X, int Y)[] points)
    {
        var result = new Point[points.Length];
        for (int i = 0; i < points.Length; i++)
            result[i] = new Point(points[i].X, points[i].Y);
        return result;
    }

    private static readonly System.Collections.Generic.Dictionary<int, (char Kind, Point[] Waypoints, int MoveEvery, bool Loop)[]> PatrolTable = new()
    {
        // Level 3: a guard paces the column by the door.
        [2] = new[] { ('G', Way((13, 2), (13, 8)), 1, false) },

        // Level 4: a guard circles the middle of the chamber.
        [3] = new[] { ('G', Way((9, 5), (12, 5), (12, 7), (9, 7)), 1, true) },

        // Level 5: the Kidnapper circles the whole approach to the door.
        [4] = new[] { ('K', Way((11, 6), (14, 6), (14, 8), (11, 8)), 1, true) },
    };

    public static readonly string[] EpisodeNames = { "THE AWAKENING" };

    public static readonly string[][] All =
    {

        // 1: The dungeon entrance. No guards, nothing that can hurt Ben. Gold carpet leads along the way out. The round teal button opens
        // the door only while someone stands on it: step on it and keep walking east to the door; Ben's echo, a few steps behind,
        // lands on the button and holds the door open.
        new[]
        {
            "###T#####T#######T##",
            "#....#.........#...#",
            "#,...#.....##..#...#",
            "#....#..#.,##..#..,#",
            "#.=..#..#......#...#",
            "#....#..#......#.=.#",
            "#....#.........T...#",
            "#.B~~.~~~~~P~~~D~X.#",
            "#....#.........T...#",
            "#....#..#......#...#",
            "#....#=.#......#...#",
            "#.,..#..#..##..#..,#",
            "#....#...,.##,.#...#",
            "#....#.........#...#",
            "######T######T######",
        },

        // 2: The twin hall. Stone pillars split the hall into two paths. The door needs BOTH the square plate (it answers to a crate)
        // and the round button (it answers to a character): push the crate east onto the square plate, then step on the round button
        // and keep walking to the door, so Ben's echo, three steps behind, is on the button as he reaches the doorway. (Z / X change the
        // echo's delay.)
        new[]
        {
            "###T######T####T####",
            "#....#...,.....#...#",
            "#,.~.#..O=.....#.~.#",
            "#..~.#..#..OO..#.~.#",
            "#..~.#..O......#.~.#",
            "#..~.T....O..P.#.~.#",
            "#..~.#.........#.~.#",
            "#.B...~~C..Q...D.X.#",
            "#..~.#.........#.~.#",
            "#..~.T....O..,.#.~.#",
            "#..~.#..O......#.~.#",
            "#..~.#..#..OO..#.~.#",
            "#.,~.#..O.....=#.~,#",
            "#....#...,....,#...#",
            "##########T#####T###",
        },

        // 3: The guard's corridor. A guard paces the column by the door, and the red carpet marks his beat. The square plate takes a
        // crate: push it along the corridor onto the plate to open the door. His red cone sweeps the chamber; crates block his sight,
        // so what lies behind one is hidden, but a guard that sees a crate someone has moved stops to stare at it ("!").
        new[]
        {
            "###T########T#T##T##",
            "#....#####.....#...#",
            "#,.~.#####.,#~.#...#",
            "#..~.#####..#~.#...#",
            "#..~.T####..#~.#..,#",
            "#..~.#####=..~.#...#",
            "#..~.##T#T...~.#...#",
            "#.B...~C~~..Q~.D.X.#",
            "#..~.##T#T.....#...#",
            "#..~.#####=....#.=.#",
            "#..~.T####..#..#...#",
            "#..~.#####..#..#..,#",
            "#..~,#####.,#.,#...#",
            "#....#####.....#...#",
            "############T####T##",
        },

        // 4: The storeroom. The door is locked until a crate is pushed onto the E switch (it stays on for good), and the round button
        // needs Ben's echo. A guard circles the middle of the room; spare crates make cover to hide behind, and a moved crate that the
        // guard sees makes him stop and stare. Time the pushes between his sweeps.
        new[]
        {
            "###T######T###T##T##",
            "#....####......#...#",
            "#.~..####.=..=,#...#",
            "#=~.,####..#.C.#.=.#",
            "#.~..####..#...#...#",
            "#.~..##T#....#.#.,.#",
            "#.~..####.....=#...#",
            "#.B~~..C.......D.X.#",
            "#.~..####......#...#",
            "#.~..##T#=..E#.#...#",
            "#.~..####.,#..P#...#",
            "#=~..####..#...#.=,#",
            "#,~..####.=..=.#...#",
            "#....####..,...#...#",
            "###########T####T###",
        },

        // 5: The Kidnapper's arena. He circles the approach to the door (the runes mark his ground); his purple cone points ahead
        // (1 wide, then 3 wide) and he also notices the four tiles touching him, so keep out of his front and slip past when he turns
        // away. Park the crate on the square plate, use the echo on the round button, and send the echo into his sight as a decoy
        // (he stops to stare at it) so Ben can slip by to the exit; it is quicker than waiting for him to look away.
        new[]
        {
            "###T####T###T#T##T##",
            "#....#...,.....#...#",
            "#,.~.#....#.=,.#.~.#",
            "#..~.#..=.#....#,~.#",
            "#.,~.#....T....#.~.#",
            "#..~.#......#P.#.~.#",
            "#.=~.#T#...r..r#.~.#",
            "#..~.###.......D.X.#",
            "#..~.##TC..r..r#.~.#",
            "#..~.#......##.#.~.#",
            "#..~~.....T....#=~.#",
            "#.B..#....#=...#.~.#",
            "#..~.#..Q,#.,..#.~,#",
            "#....#.........#...#",
            "####T####T#####T#T##",
        },
    };
}
