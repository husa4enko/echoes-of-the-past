using System;
using System.Collections.Generic;
using EchoesOfThePast.Art;
using EchoesOfThePast.Entities;
using EchoesOfThePast.Objects;
using EchoesOfThePast.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace EchoesOfThePast.Gameplay;

public enum SessionStatus
{
    Playing,
    Completed,
    Caught,
}

/// <summary>
/// One attempt at one level: the map, Ben, his echo, the doors, crates and sentries, and every rule that ties them
/// together. It knows nothing about menus or screens; the owner calls <see cref="Update"/> and reads <see cref="Status"/>.
/// The world advances per Ben step: when a step lands, the echo replays one, doors follow the plates, sentries move, and only then is anyone judged.
/// </summary>
public sealed class LevelSession
{
    /// <summary>
    /// If true the Phantom is caught like Ben (the stealth rule); if false it is only a decoy that
    /// distracts guards instead (a guard that sees it stares at it and stops moving or turning).
    /// </summary>
    private static readonly bool PhantomCausesCaught = false;

    private readonly Player _ben;
    private readonly MovementHistory _history = new();
    private readonly Phantom _phantom;
    private readonly List<PressurePlate> _plates = new();
    private readonly Door _door;
    private readonly CrateField _crates = new();
    private readonly List<Guard> _guards = new();

    /// <summary>Set when a patrolling sentry steps onto Ben or he bumps into one; the next guard check catches him.</summary>
    private Point? _crushedAt;
    private bool _exitReached;

    public LevelSession(int index, int phantomDelay)
    {
        Index = index;
        try
        {
            Map = TileMap.FromStrings(Levels.Layout(index));
        }
        catch (ArgumentException e)
        {
            throw new InvalidOperationException($"Level {index + 1} is invalid: {e.Message}", e);
        }

        Map.Theme = Levels.Theme(index);

        foreach (Point tile in Map.CrateTiles)
            _crates.Add(tile);

        (int Every, int Quarters)[] rules = Levels.GuardRules(index);
        for (int i = 0; i < Map.GuardPlacements.Count; i++)
        {
            (Point tile, Direction facing) = Map.GuardPlacements[i];
            (int every, int quarters) = i < rules.Length ? rules[i] : (0, 1);
            _guards.Add(new Guard(tile, facing, every, quarters));
        }

        foreach ((CharacterKind kind, Point[] route, int moveEvery, bool loop) in Levels.Patrols(index))
            _guards.Add(new Guard(kind, route, moveEvery, loop));

        _ben = new Player(Map.PlayerSpawn);
        _phantom = new Phantom(Map.PlayerSpawn, _history, phantomDelay);
        foreach (Point plateTile in Map.PlateTiles)
            _plates.Add(new PressurePlate(plateTile, Map.SquarePlateTiles.Contains(plateTile), Map.SwitchTiles.Contains(plateTile)));
        _door = new Door(Map.DoorTile.Value);
        RefreshDoor();
        _ben.Stepped += OnBenStepped;
    }

    public int Index { get; }
    public TileMap Map { get; }
    public SessionStatus Status { get; private set; } = SessionStatus.Playing;

    /// <summary>The tile where Ben was caught, once <see cref="Status"/> is <see cref="SessionStatus.Caught"/>.</summary>
    public Point? CaughtAt { get; private set; }

    /// <summary>Steps Ben has taken so far (the exit step does not count).</summary>
    public int StepsTaken { get; private set; }

    /// <summary>The tile Ben occupies; while a step is in progress this is already the destination.</summary>
    public Point BenTile => _ben.GridPosition;

    public bool IsBenMoving => _ben.IsMoving;

    public int PhantomDelay
    {
        get => _phantom.Delay;
        set => _phantom.Delay = value;
    }

    public void Update(GameTime gameTime, KeyboardState keyboard)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // Plate and door are refreshed first so Ben's step this frame sees the current door state.
        RefreshDoor();
        EjectBenFromClosedDoor();

        _ben.Update(gameTime, keyboard, Map, BenMayEnter);
        if (_exitReached)
            CompleteLevel();
        _phantom.Update(gameTime, PhantomMayEnter);
        _crates.Update(dt);
        _door.Tick(dt);

        // Buttons and the door follow where everyone stands now, so the cones see the door as it is after this step.
        RefreshDoor();

        // While a step is in progress Ben (and anything he pushed) is between two settled states, so nobody is
        // judged then; the check runs when the step lands (OnBenStepped) and whenever Ben stands still.
        if (Status == SessionStatus.Playing && !_ben.IsMoving)
            UpdateGuards();
    }

    /// <summary>Keeps crates sliding while the world is otherwise frozen (the level-complete banner).</summary>
    public void UpdateIdle(float dt) => _crates.Update(dt);

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel)
    {
        Map.Draw(spriteBatch, pixel);
        foreach (PressurePlate plate in _plates)
            plate.Draw(spriteBatch, pixel);
        _door.Draw(spriteBatch, pixel);
        _crates.Draw(spriteBatch, pixel);
        foreach (Guard guard in _guards)
            guard.Draw(spriteBatch, pixel, Map, BlocksSight);
        _phantom.Draw(spriteBatch, pixel);
        _ben.Draw(spriteBatch, pixel);
    }

    // ---- Step pipeline ----------------------------------------------------------------------------------

    private void OnBenStepped(Point tile)
    {
        if (tile == Map.ExitTile)
        {
            _exitReached = true;
            return;
        }

        _history.Record(tile);
        StepsTaken++;

        // Everything that happens because of this step is settled right here, before Ben's next step (and any crate
        // push) can begin: the echo moves, the door follows the buttons, the sentries move, and only then is anyone caught.
        _phantom.OnBenStepCompleted();
        _phantom.StepNow(PhantomMayEnter);
        RefreshDoor();
        foreach (Guard guard in _guards)
            guard.OnBenStep(CanSentryEnter);
        RefreshDoor();

        if (Status == SessionStatus.Playing)
            UpdateGuards();
    }

    /// <summary>
    /// Ben reached the exit. He stays on it, his history is wiped and the Phantom is reset onto his tile.
    /// </summary>
    private void CompleteLevel()
    {
        _exitReached = false;
        Status = SessionStatus.Completed;

        // Snap Ben onto the exit; this also cancels any step a held key started this frame.
        _ben.Teleport(Map.ExitTile.Value);
        _history.Clear();
        _phantom.Reset(_ben.GridPosition);

        // Nobody is on the plate any more, so the door shuts behind Ben.
        RefreshDoor();
    }

    /// <summary>Re-reads every plate, then opens or shuts the door to match.</summary>
    private void RefreshDoor()
    {
        foreach (PressurePlate plate in _plates)
            plate.Update(_ben, _phantom, _crates);
        _door.Update(_plates, Map);
    }

    // ---- Guards -----------------------------------------------------------------------------------------

    /// <summary>
    /// The Phantom in a guard's sight distracts it (it stares at the Phantom and stops turning).
    /// Ben in anyone's sight gets him caught.
    /// </summary>
    private void UpdateGuards()
    {
        if (_crushedAt is Point crushed)
        {
            BeCaught(crushed);
            return;
        }

        foreach (Guard guard in _guards)
        {
            guard.Calm();
            if (guard.WatchedTiles(Map, BlocksSight).Contains(_phantom.GridPosition))
            {
                guard.Distract(_phantom.GridPosition);
                if (PhantomCausesCaught)
                {
                    BeCaught(_phantom.GridPosition);
                    return;
                }
            }
        }

        // A sentry that sees a crate someone has moved gets suspicious: it stares at it for a few steps.
        foreach (Guard guard in _guards)
        {
            if (guard.Distracted || guard.Suspicious)
                continue;

            List<Point> watched = guard.WatchedTiles(Map, BlocksSight);
            foreach (Crate crate in _crates.Crates)
            {
                if (crate.Displaced && watched.Contains(crate.Tile) && guard.Suspect(crate.Tile))
                    break;
            }
        }

        foreach (Guard guard in _guards)
        {
            if (guard.WatchedTiles(Map, BlocksSight).Contains(_ben.SettledTile))
            {
                BeCaught(_ben.SettledTile);
                return;
            }
        }
    }

    private void BeCaught(Point at)
    {
        Status = SessionStatus.Caught;
        CaughtAt = at;
    }

    // ---- Movement rules (handed to the entities as delegates) -------------------------------------------

    /// <summary>Where a patrolling sentry may step: open floor with nothing and nobody on it.</summary>
    private bool CanSentryEnter(Point tile)
    {
        if (!Map.IsWalkable(tile) || _crates.CrateAt(tile) != null || IsNpcAt(tile))
            return false;

        // Walking into Ben isn't blocked: it catches him. The echo is a ghost and is simply walked through.
        if (_ben.SettledTile == tile)
            _crushedAt = tile;
        return true;
    }

    /// <summary>Walls, closed doors, crates and people block a guard's line of sight.</summary>
    private bool BlocksSight(Point tile) =>
        !Map.IsWalkable(tile) || _crates.CrateAt(tile) != null || IsNpcAt(tile);

    /// <summary>Guards and the Kidnapper are solid: nothing walks or is pushed onto their tile.</summary>
    private bool IsNpcAt(Point tile)
    {
        foreach (Guard guard in _guards)
        {
            if (guard.Tile == tile)
                return true;
        }

        return false;
    }

    /// <summary>Ben can't walk onto the Phantom, and walking onto a crate pushes it if it can move.</summary>
    private bool BenMayEnter(Point tile, Direction direction)
    {
        // Bumping into a sentry gets Ben caught on the spot.
        if (IsNpcAt(tile))
        {
            _crushedAt = tile;
            return false;
        }

        return _phantom.GridPosition != tile && _crates.TryEnter(Map, tile, direction, IsBlockedForCrate);
    }

    /// <summary>
    /// The echo walks through Ben and the sentries (it is a ghost, so it never gets held up and stays exactly
    /// Delay steps behind); it only pushes crates in its path the same way he does.
    /// </summary>
    private bool PhantomMayEnter(Point tile, Direction direction) =>
        _crates.TryEnter(Map, tile, direction, IsBlockedForCrate);

    /// <summary>
    /// Where a pushed crate may not land: on Ben or the Phantom, on the exit (it could seal it), or
    /// in the doorway (the door would shut on it).
    /// </summary>
    private bool IsBlockedForCrate(Point tile) =>
        IsNpcAt(tile) || _ben.Occupies(tile) || _phantom.Occupies(tile) || tile == _door.Tile || tile == Map.ExitTile;

    /// <summary>
    /// If the door shut while Ben was standing in it, nudge him on in the direction he was facing,
    /// or back out the way he came. If neither side is free he stays put until the door reopens.
    /// </summary>
    private void EjectBenFromClosedDoor()
    {
        if (_door.IsOpen || _ben.IsMoving || _ben.GridPosition != _door.Tile)
            return;

        Direction forward = _ben.Facing;
        if (!_ben.TryEject(forward, Map, BenMayEnter))
            _ben.TryEject(forward.Opposite(), Map, BenMayEnter);
    }
}
