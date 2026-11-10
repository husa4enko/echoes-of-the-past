using System;
using EchoesOfThePast.Entities;
using EchoesOfThePast.Gameplay;
using EchoesOfThePast.World;
using Xunit;

namespace EchoesOfThePast.Tests;

public class LayoutTests
{
    [Fact]
    public void Every_level_loads_with_the_same_room_size()
    {
        var size = Levels.WorldPixelSize;

        for (int level = 0; level < Levels.Count; level++)
        {
            var session = new LevelSession(level, Phantom.DefaultDelay);
            Assert.Equal(size.X, session.Map.PixelWidth);
            Assert.Equal(size.Y, session.Map.PixelHeight);
        }
    }

    [Theory]
    [InlineData("#######", "#.PDX.#", "#######")] // no spawn
    [InlineData("#######", "#B.DX.#", "#######")] // no plate
    [InlineData("#######", "#BP.X.#", "#######")] // no door
    [InlineData("#######", "#BPD..#", "#######")] // no exit
    public void A_layout_missing_a_required_tile_is_rejected(params string[] rows)
    {
        Assert.Throws<ArgumentException>(() => TileMap.FromStrings(rows));
    }

    [Fact]
    public void A_layout_with_everything_loads()
    {
        TileMap map = TileMap.FromStrings("#######", "#BPDX.#", "#######");

        Assert.NotNull(map.DoorTile);
        Assert.NotNull(map.ExitTile);
    }

    [Theory]
    [InlineData(-5, Phantom.MinDelay)]
    [InlineData(4, 4)]
    [InlineData(99, Phantom.MaxDelay)]
    public void The_echo_delay_stays_within_its_limits(int requested, int expected)
    {
        var phantom = new Phantom(new Microsoft.Xna.Framework.Point(1, 1), new MovementHistory(), requested);

        Assert.Equal(expected, phantom.Delay);
    }
}
