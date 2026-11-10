using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using EchoesOfThePast.Gameplay;
using EchoesOfThePast.World;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Logging;
using Xunit;
 
namespace EchoesOfThePast.Tests;
 
/// <summary>
/// Four API tests. A small in-memory HTTP API (built below with ASP.NET minimal APIs) serves the real levels
/// and replays submitted moves through the real <see cref="LevelSession"/>; each test calls it over HTTP and
/// checks the exact expected result.
/// </summary>
public class ApiTests
{
    [Fact]
    public async Task Get_level_1_returns_its_grid()
    {
        using HttpClient client = await StartApiAsync();
 
        HttpResponseMessage response = await client.GetAsync("/api/levels/1");
 
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        JsonElement level = await ReadJsonAsync(response);
        Assert.Equal(20, level.GetProperty("width").GetInt32());
        Assert.Equal(15, level.GetProperty("height").GetInt32());
        Assert.Equal(2, level.GetProperty("spawn").GetProperty("x").GetInt32());
        Assert.Equal(7, level.GetProperty("spawn").GetProperty("y").GetInt32());
        Assert.Equal(17, level.GetProperty("exit").GetProperty("x").GetInt32());
        Assert.Equal(7, level.GetProperty("exit").GetProperty("y").GetInt32());
    }
 
    [Fact]
    public async Task Submit_level_1_solution_completes_the_level()
    {
        using HttpClient client = await StartApiAsync();
 
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/levels/1/submit",
            new { playerName = "ben", moves = "RRRRRRRRRRRRRR" });
 
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        JsonElement result = await ReadJsonAsync(response);
        Assert.Equal("completed", result.GetProperty("result").GetString());
        Assert.Equal(14, result.GetProperty("stepsTaken").GetInt32());
    }
 
    [Fact]
    public async Task Submit_walking_into_the_level_4_guard_is_caught()
    {
        using HttpClient client = await StartApiAsync();
 
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/levels/4/submit",
            new { playerName = "ben", moves = "UUDRDRRRRRRRRRRR" });
 
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        JsonElement result = await ReadJsonAsync(response);
        Assert.Equal("caught", result.GetProperty("result").GetString());
        Assert.Equal(14, result.GetProperty("stepsTaken").GetInt32());
        Assert.Equal(12, result.GetProperty("caughtAt").GetProperty("x").GetInt32());
        Assert.Equal(7, result.GetProperty("caughtAt").GetProperty("y").GetInt32());
    }
 
    [Fact]
    public async Task Leaderboard_lists_fewest_steps_first()
    {
        using HttpClient client = await StartApiAsync();
        await client.PostAsJsonAsync("/api/levels/1/submit", new { playerName = "slow", moves = "RRRRRRURRDRRRRRRR" }); // 16 steps
        await client.PostAsJsonAsync("/api/levels/1/submit", new { playerName = "fast", moves = "RRRRRRRRRRRRRR" });    // 14 steps
 
        HttpResponseMessage response = await client.GetAsync("/api/leaderboard?levelId=1");
 
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        JsonElement[] entries = (await ReadJsonAsync(response)).EnumerateArray().ToArray();
        Assert.Equal(2, entries.Length);
        Assert.Equal("fast", entries[0].GetProperty("playerName").GetString());
        Assert.Equal(14, entries[0].GetProperty("steps").GetInt32());
        Assert.Equal("slow", entries[1].GetProperty("playerName").GetString());
        Assert.Equal(16, entries[1].GetProperty("steps").GetInt32());
    }
 
    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
 
    private record Submission(string PlayerName, string Moves);
 
    /// <summary>Starts a fresh in-memory API (empty leaderboard) and returns a client for it.</summary>
    private static async Task<HttpClient> StartApiAsync()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders(); // keep test output quiet
        WebApplication app = builder.Build();
        var leaderboard = new List<(int Level, string Name, int Steps)>();
 
        app.MapGet("/api/levels/{id:int}", (int id) =>
        {
            TileMap map = TileMap.FromStrings(Levels.Layout(id - 1));
            return Results.Ok(new
            {
                width = map.Width,
                height = map.Height,
                spawn = new { x = map.PlayerSpawn.X, y = map.PlayerSpawn.Y },
                exit = new { x = map.ExitTile!.Value.X, y = map.ExitTile.Value.Y },
            });
        });
 
        app.MapPost("/api/levels/{id:int}/submit", (int id, Submission submission) =>
        {
            var driver = new SessionDriver(id - 1, Entities.Phantom.DefaultDelay, realFrameRate: false);
            SessionStatus status = driver.Play(submission.Moves);
            LevelSession session = driver.Session;
            if (status == SessionStatus.Completed)
                leaderboard.Add((id, submission.PlayerName, session.StepsTaken));
 
            return Results.Ok(new
            {
                result = status switch { SessionStatus.Completed => "completed", SessionStatus.Caught => "caught", _ => "incomplete" },
                stepsTaken = session.StepsTaken,
                caughtAt = session.CaughtAt is { } at ? new { x = at.X, y = at.Y } : null,
            });
        });
 
        app.MapGet("/api/leaderboard", (int levelId) =>
            Results.Ok(leaderboard.Where(e => e.Level == levelId).OrderBy(e => e.Steps)
                .Select(e => new { playerName = e.Name, steps = e.Steps })));
 
        await app.StartAsync();
        return app.GetTestClient();
    }
}
 