# Claude.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

"Echoes of the Past" is a 2D grid-based stealth/puzzle game built with MonoGame 3.8 (DesktopGL) on .NET 9. Ben walks a tile grid; an "echo" (the Phantom) replays his path N steps behind him (N adjustable in-game with Z/X, 1–10). The echo can hold pressure plates, push crates, and distract guards, but is not itself a fail condition (`LevelSession.PhantomCausesCaught = false`). There is no git repo.

## Commands

- Build: `dotnet build` (fails to copy the `.exe` if the game is still running; build with `-o <dir>` to check compilation anyway)
- Run: `dotnet run` (or the "C#: EchoesOfThePast Debug" launch config in `.vscode/launch.json`)
- Test: `dotnet test tests/EchoesOfThePast.Tests` (xUnit). Run one test with `--filter "FullyQualifiedName~Level_is_completed"`.
- `Content/Content.mgcb` is empty: all art is generated in code, so the MGCB content pipeline has nothing to build.

## Architecture

A single project, layered so that nothing below `Game1` knows about screens or the window.

- **`Game1.cs`** is only the MonoGame host: window setup, per-frame `InputState` polling, the `ViewTransform`, and delegating `Update`/`Draw` to the current `Screen`.
- **`Core/`** is the framework layer: `Screen` (abstract: `Update`, `Draw`, `OnShown`, `CapturesMouse`), `ScreenManager` (one registered instance per screen type; switch with `Context.Screens.Show<T>()`, which returns the screen so callers can configure it, e.g. `Show<GameplayScreen>().StartLevel(i)`), `GameContext` (world size, screen manager, quit action, and the echo-delay setting that outlives a level), `InputState` (keyboard plus mouse already in world pixels; `Pressed` is edge-triggered) and `ViewTransform` (integer scale + letterbox; window is fixed 1280x960 over a 320x240 world).
- **`Screens/`** has one class per screen: `MainMenuScreen`, `CreditsScreen`, `LevelSelectScreen`, `GameplayScreen`. `GameplayScreen` runs a private Playing/Paused/LevelComplete/Caught mode and owns the overlays; it reads input and reacts to `LevelSession.Status`. To add a screen: subclass `Screen`, register it in `Game1.Initialize`.
- **`Gameplay/LevelSession.cs`** holds all level rules for one attempt (Ben, echo, plates, door, crates, sentries). It is UI-free: the owner calls `Update` and reads `Status` (Playing/Completed/Caught). Entities don't know about each other; `LevelSession` wires them with delegates (`BenMayEnter`, `PhantomMayEnter`, `CanSentryEnter`, `BlocksSight`, `IsBlockedForCrate`).
- **`World/`**: `Levels` (ASCII layouts, patrol/turn/hint tables), `TileMap` (parses and validates layouts), `Grid`. **`Entities/`**: Player, Phantom, Guard (also the Kidnapper), Vision, MovementHistory. **`Objects/`**: Door, PressurePlate, Crate, CrateField (owns the push rule). **`UI/`**: widgets (menus, `OverlayMenu`, `LevelHints`, `CaughtOverlay`, `PixelText`, `CursorLock`). **`Art/`**: `SpriteBank` bakes pixel-art strings from `SpriteData`/`TileArt` into 16x16 textures at start-up; draw by sprite name.

**Step-driven (turn-like) simulation.** The world advances per Ben step, not per frame. `Player.Stepped` → `LevelSession.OnBenStepped` records the tile into `MovementHistory`, then synchronously: Phantom replays a step (`StepNow`), plates/door refresh, guards advance (`OnBenStep`), then `UpdateGuards` judges catching. Per-frame `Update` only animates and, while Ben is stationary, re-runs `UpdateGuards`. Keep new mechanics inside this ordering; nobody is judged while Ben is mid-step.

**Phantom** never reads input or checks walls; it dequeues Ben's recorded tiles once the history is longer than `Delay`, and waits if `tryEnter` refuses (e.g. an unpushable crate).

**Levels are ASCII** (`World/Levels.cs`): legend is in the `Levels` doc comment (`#` wall, `B` spawn, `P`/`Q`/`E` plates/switch, `D` door, `X` exit, `C` crate, `G`/arrows fixed guards, plus decoration chars). `TileMap.FromStrings` throws if a layout lacks `B`, a plate, `D` or `X`; `LevelSession` wraps that with the level number. Patrolling sentries (incl. the Kidnapper) and fixed-guard turn rules are NOT in the layout; they come from `Levels.Patrols` / `Levels.GuardRules` keyed by level index, and tutorial text from `Levels.Hints`. `Levels.Episodes`/`PerEpisode` control how many levels are playable (currently only chapter 1, 5 levels).

## Tests

`tests/EchoesOfThePast.Tests` drives a real `LevelSession` headlessly through `SessionDriver` (move letters U/D/L/R, one deliberate step each; it waits out the automatic nudge out of a shut door). `LevelSolutionTests` holds one recorded solution per level and replays it at 60 fps and with long frames; `Every_level_has_a_recorded_solution` fails when `Levels.Count` grows without a new line. After changing a level, its rules or guard behaviour, re-solve the level and update its string. The main csproj excludes `tests\**` from its source glob.
