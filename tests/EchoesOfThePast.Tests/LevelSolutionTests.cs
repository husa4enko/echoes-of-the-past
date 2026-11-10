using System.Collections.Generic;
using EchoesOfThePast.Entities;
using EchoesOfThePast.Gameplay;
using EchoesOfThePast.World;
using Xunit;

namespace EchoesOfThePast.Tests;

/// <summary>
/// Every level must be winnable. Each level has a recorded solution (U, D, L, R: one step of Ben each, with the
/// echo at its default delay) that is replayed through the real <see cref="LevelSession"/>.
/// When a level changes, re-solve it and update its line here; a missing line fails the build.
/// </summary>
public class LevelSolutionTests
{
    private static readonly string[] Solutions =
    {
        // 1 The entrance: walk straight east; the echo lands on the button and holds the door as Ben passes.
        "RRRRRRRRRRRRRR",

        // 2 The twin hall: push the crate onto the square plate, then lead the echo onto the round button.
        "RRRRRRRRURURRDDRRR",

        // 3 The guard's corridor: bring the crate down the corridor onto the plate, past the pacing guard.
        "UUUUDRDDDRRRRRRRRDRRURRRR",

        // 4 The storeroom: latch the E switch with a crate, then cross the room while the guard circles.
        "UUDRDRRRRRRRURDDLDRLDDDRRUURRUUURR",

        // 5 The Kidnapper's arena: park the crate, use the echo on the button and slip past.
        "URRRRRRRUUULDDDDURUUUURRRRUDDRRR",
    };

    public static IEnumerable<object[]> EveryLevelAtBothFrameRates()
    {
        for (int level = 0; level < Levels.Count; level++)
        {
            yield return new object[] { level, false };
            yield return new object[] { level, true };
        }
    }

    [Fact]
    public void Every_level_has_a_recorded_solution()
    {
        Assert.Equal(Levels.Count, Solutions.Length);
    }

    [Theory]
    [MemberData(nameof(EveryLevelAtBothFrameRates))]
    public void Level_is_completed_by_its_recorded_moves(int level, bool realFrameRate)
    {
        var driver = new SessionDriver(level, Phantom.DefaultDelay, realFrameRate);

        SessionStatus status = driver.Play(Solutions[level]);

        Assert.Equal(SessionStatus.Completed, status);
    }

    [Theory]
    [MemberData(nameof(EveryLevelAtBothFrameRates))]
    public void Level_is_not_completed_without_the_last_move(int level, bool realFrameRate)
    {
        var driver = new SessionDriver(level, Phantom.DefaultDelay, realFrameRate);
        string moves = Solutions[level];

        SessionStatus status = driver.Play(moves.Substring(0, moves.Length - 1));

        Assert.Equal(SessionStatus.Playing, status);
    }
}
