using System;
using System.Collections.Generic;
using EchoesOfThePast.Art;
using EchoesOfThePast.Entities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EchoesOfThePast.World;

public enum TileType
{
    Floor,
    Wall,
}

/// <summary>
/// A rectangular grid of tiles. Coordinates outside the map are treated as solid.
/// </summary>
public class TileMap
{
    private readonly TileType[,] _tiles;

    public int Width { get; }
    public int Height { get; }

    /// <summary>Index into <see cref="TileArt.Themes"/>: which wall and floor art this map is drawn with.</summary>
    public int Theme { get; set; }
    public int PixelWidth => Width * Grid.TileSize;
    public int PixelHeight => Height * Grid.TileSize;

    /// <summary>Spawn tile for Ben, set by a 'B' in <see cref="FromStrings"/>.</summary>
    public Point PlayerSpawn { get; private set; }

    /// <summary>Every pressure plate ('P' round, 'Q' square); the door opens only while all of them are pressed.</summary>
    public List<Point> PlateTiles { get; } = new();

    /// <summary>The plates that are square ('Q', crates only); the rest of <see cref="PlateTiles"/> are round buttons ('P', characters only).</summary>
    public HashSet<Point> SquarePlateTiles { get; } = new();

    /// <summary>The crate switches ('E'): square plates that stay pressed for good once a crate has been on them.</summary>
    public HashSet<Point> SwitchTiles { get; } = new();

    /// <summary>Tiles marked 'D' and 'X' in <see cref="FromStrings"/>; null if the map has none.</summary>
    public Point? DoorTile { get; private set; }
    public Point? ExitTile { get; private set; }

    /// <summary>Starting tiles of the crates, from 'C' in <see cref="FromStrings"/>.</summary>
    public List<Point> CrateTiles { get; } = new();

    /// <summary>Fixed guards ('G' or an arrow ^ v &lt; &gt; for their facing). Patrolling sentries come from <see cref="Levels.Patrols"/> instead.</summary>
    public List<(Point Tile, Direction Facing)> GuardPlacements { get; } = new();

    /// <summary>Non-blocking floor props (',' debris, '=' grate, '~' carpet) and wall torches ('T'), drawn over the tiles.</summary>
    private readonly Dictionary<Point, char> _props = new();

    public TileMap(int width, int height)
    {
        Width = width;
        Height = height;
        _tiles = new TileType[width, height];
    }

    /// <summary>
    /// Builds a map from text rows: '#' = wall, 'B' = Ben's spawn, 'P' = round button (Ben and the Phantom), 'Q' = square plate (crates), 'E' = crate switch (latches the door open), 'D' = door,
    /// 'X' = exit, 'C' = crate, 'G' / '^' 'v' '<' '>' = a guard (facing down by default, or the arrow),
    /// all of these sit on floor. ',' '=' '~' are decorations on floor (debris, vent / grate, carpet),
    /// 'T' is a wall with a torch, 'O' a stone pillar (a wall standing on floor), 'r' a faint floor rune. Anything else is floor too.
    /// </summary>
    public static TileMap FromStrings(params string[] rows)
    {
        if (rows.Length == 0)
            throw new ArgumentException("Map must have at least one row.", nameof(rows));

        int width = rows[0].Length;
        var map = new TileMap(width, rows.Length);
        bool hasSpawn = false;

        for (int y = 0; y < rows.Length; y++)
        {
            if (rows[y].Length != width)
                throw new ArgumentException($"Row {y} has length {rows[y].Length}, expected {width}.", nameof(rows));

            for (int x = 0; x < width; x++)
            {
                char c = rows[y][x];
                map._tiles[x, y] = c == '#' || c == 'T' || c == 'O' ? TileType.Wall : TileType.Floor;
                if (c == ',' || c == '=' || c == '~' || c == 'T' || c == 'O' || c == 'r')
                    map._props[new Point(x, y)] = c;
                switch (c)
                {
                    case 'B': map.PlayerSpawn = new Point(x, y); hasSpawn = true; break;
                    case 'P': map.PlateTiles.Add(new Point(x, y)); break;
                    case 'Q': map.PlateTiles.Add(new Point(x, y)); map.SquarePlateTiles.Add(new Point(x, y)); break;
                    case 'E': map.PlateTiles.Add(new Point(x, y)); map.SquarePlateTiles.Add(new Point(x, y)); map.SwitchTiles.Add(new Point(x, y)); break;
                    case 'D': map.DoorTile = new Point(x, y); break;
                    case 'X': map.ExitTile = new Point(x, y); break;
                    case 'C': map.CrateTiles.Add(new Point(x, y)); break;
                    case 'G': case 'v': map.GuardPlacements.Add((new Point(x, y), Direction.Down)); break;
                    case '^': map.GuardPlacements.Add((new Point(x, y), Direction.Up)); break;
                    case '<': map.GuardPlacements.Add((new Point(x, y), Direction.Left)); break;
                    case '>': map.GuardPlacements.Add((new Point(x, y), Direction.Right)); break;
                }
            }
        }

        if (!hasSpawn)
            throw new ArgumentException("Map has no Ben spawn ('B').", nameof(rows));
        if (map.PlateTiles.Count == 0)
            throw new ArgumentException("Map has no plate ('P', 'Q' or 'E').", nameof(rows));
        if (map.DoorTile == null)
            throw new ArgumentException("Map has no door ('D').", nameof(rows));
        if (map.ExitTile == null)
            throw new ArgumentException("Map has no exit ('X').", nameof(rows));

        return map;
    }

    public bool InBounds(Point tile) =>
        tile.X >= 0 && tile.Y >= 0 && tile.X < Width && tile.Y < Height;

    public TileType GetTile(Point tile) => InBounds(tile) ? _tiles[tile.X, tile.Y] : TileType.Wall;

    public void SetTile(Point tile, TileType type)
    {
        if (InBounds(tile))
            _tiles[tile.X, tile.Y] = type;
    }

    public bool IsWalkable(Point tile) => GetTile(tile) != TileType.Wall;

    private static int Hash(int x, int y, int salt) => (int)((uint)(x * 73856093 ^ y * 19349663 ^ salt * 83492791) >> 8);

    /// <summary>Draws floors, walls and the exit from the sprite bank in this map's theme, with a little floor variation.</summary>
    public void Draw(SpriteBatch spriteBatch, Texture2D pixel)
    {
        TileTheme theme = TileArt.Themes[Theme];

        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                var tile = new Point(x, y);
                bool pillar = _props.TryGetValue(tile, out char prop) && prop == 'O';
                string sprite;
                if (tile == ExitTile)
                    sprite = "exit";
                else if (_tiles[x, y] == TileType.Wall && !pillar)
                    sprite = $"t{Theme}_" + (IsWalkable(new Point(x, y + 1)) ? "wall_lip" : "wall") + Hash(x, y, 5) % TileArt.WallVariants;
                else if (theme.Floor == FloorStyle.Checker)
                    sprite = $"t{Theme}_floor" + ((x + y) % 2 * 4 + Hash(x, y, 3) % 4);
                else
                    sprite = $"t{Theme}_floor" + Hash(x, y, 3) % TileArt.FloorVariants;

                SpriteBank.Draw(spriteBatch, sprite, Grid.TileBounds(tile).Location);
                if (prop != default)
                    DrawProp(spriteBatch, pixel, tile, prop, theme);
            }
        }
    }

    private void DrawProp(SpriteBatch spriteBatch, Texture2D pixel, Point tile, char prop, TileTheme theme)
    {
        Rectangle r = Grid.TileBounds(tile);
        int h = Hash(tile.X, tile.Y, 11);
        switch (prop)
        {
            case '~': // carpet: a red runner with a gold edge
                spriteBatch.Draw(pixel, new Rectangle(r.X + 1, r.Y, r.Width - 2, r.Height), new Color(112, 30, 38));
                spriteBatch.Draw(pixel, new Rectangle(r.X + 1, r.Y, 1, r.Height), new Color(170, 130, 60));
                spriteBatch.Draw(pixel, new Rectangle(r.Right - 2, r.Y, 1, r.Height), new Color(170, 130, 60));
                spriteBatch.Draw(pixel, new Rectangle(r.X + 7, r.Y + 6 + h % 3, 2, 2), new Color(150, 50, 56));
                break;
            case '=': // floor grate
                spriteBatch.Draw(pixel, new Rectangle(r.X + 3, r.Y + 3, 10, 10), new Color(24, 22, 28));
                for (int i = 0; i < 4; i++)
                    spriteBatch.Draw(pixel, new Rectangle(r.X + 3, r.Y + 4 + i * 3, 10, 1), new Color(110, 108, 118));
                break;
            case ',': // scattered debris
                var stone = new Color(70, 64, 62);
                spriteBatch.Draw(pixel, new Rectangle(r.X + 2 + h % 5, r.Y + 3 + h / 5 % 5, 3, 2), stone);
                spriteBatch.Draw(pixel, new Rectangle(r.X + 9 + h / 3 % 3, r.Y + 9 + h / 7 % 3, 2, 2), new Color(88, 80, 74));
                spriteBatch.Draw(pixel, new Rectangle(r.X + 5 + h / 11 % 4, r.Y + 11, 4, 1), new Color(56, 50, 48));
                break;
            case 'T': // wall torch with a flickering flame
                int flick = (Environment.TickCount / 120 + tile.X * 3) % 3;
                spriteBatch.Draw(pixel, new Rectangle(r.X + 7, r.Y + 8, 2, 5), new Color(90, 60, 36));
                spriteBatch.Draw(pixel, new Rectangle(r.X + 6, r.Y + 6 - (flick == 0 ? 1 : 0), 4, 3), new Color(240, 150, 40));
                spriteBatch.Draw(pixel, new Rectangle(r.X + 7, r.Y + 5 - (flick == 1 ? 1 : 0), 2, 2), new Color(255, 225, 110));
                spriteBatch.Draw(pixel, new Rectangle(r.X + 3, r.Y + 3, 10, 10), new Color(255, 170, 60) * 0.10f);
                if (Theme == 4)
                {
                    // Arena torches light the floor around them, pulsing a little out of step with each other.
                    float pulse = 0.5f + 0.5f * MathF.Sin(Environment.TickCount / 260f + tile.X * 1.7f);
                    spriteBatch.Draw(pixel, new Rectangle(r.X - 6, r.Y - 2, r.Width + 12, r.Height + 14), new Color(255, 160, 60) * (0.035f + 0.035f * pulse));
                }
                break;
            case 'O': // stone pillar standing on the floor
                spriteBatch.Draw(pixel, new Rectangle(r.X + 2, r.Y + 11, 12, 3), new Color(0, 0, 0) * 0.35f);
                spriteBatch.Draw(pixel, new Rectangle(r.X + 3, r.Y + 3, 10, 10), theme.WallBase);
                spriteBatch.Draw(pixel, new Rectangle(r.X + 4, r.Y + 4, 8, 8), theme.Brick[1]);
                spriteBatch.Draw(pixel, new Rectangle(r.X + 4, r.Y + 4, 3, 8), theme.BrickLit);
                spriteBatch.Draw(pixel, new Rectangle(r.X + 10, r.Y + 4, 2, 8), theme.BrickShade);
                spriteBatch.Draw(pixel, new Rectangle(r.X + 2, r.Y + 2, 12, 3), theme.BrickLit);
                spriteBatch.Draw(pixel, new Rectangle(r.X + 2, r.Y + 11, 12, 2), theme.BrickShade);
                break;
            case 'r': // faint rune marking the boss's zone
                var rune = new Color(160, 90, 200) * 0.45f;
                for (int d = 0; d <= 3; d++)
                {
                    spriteBatch.Draw(pixel, new Rectangle(r.X + 8 + d, r.Y + 4 + d, 1, 1), rune);
                    spriteBatch.Draw(pixel, new Rectangle(r.X + 7 - d, r.Y + 4 + d, 1, 1), rune);
                    spriteBatch.Draw(pixel, new Rectangle(r.X + 8 + d, r.Y + 11 - d, 1, 1), rune);
                    spriteBatch.Draw(pixel, new Rectangle(r.X + 7 - d, r.Y + 11 - d, 1, 1), rune);
                }
                spriteBatch.Draw(pixel, new Rectangle(r.X + 7, r.Y + 7, 2, 2), rune);
                break;
        }
    }
}
