using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PilotSim.Data;
using PilotSim.Data.Models;
using PilotSim.Server.Controllers;
using PilotSim.Core;
using Xunit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.AspNetCore.SignalR;
using Moq;

namespace PilotSim.Tests;

public class SessionSummaryTests
{
    private SimDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<SimDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new SimDbContext(options);
    }

    [Fact]
    public async Task SummaryExcludesBlockedTurnScoreAndCalculatesAverages()
    {
        using var context = CreateContext();
        var session = new Session { Id = 1, ScoreTotal = 0 };
        context.Sessions.Add(session);
        await context.SaveChangesAsync();

        // Add two turns: one blocked (with score delta) and one normal
        var verdictBlocked = new InstructorVerdict(new List<string>{"ISSUE"}, new List<string>(), null, 0.2, 5, "Blocked reason", new List<ComponentScore>(), 0.2, null, null, null, false, "v1");
        var verdictNormal = new InstructorVerdict(new List<string>(), new List<string>(), null, 0.8, 10, string.Empty, new List<ComponentScore>(), 0.8, null, null, null, false, "v1");

        var blockedTurn = new Turn { SessionId = 1, Idx = 0, InstructorJson = JsonSerializer.Serialize(verdictBlocked), ScoreDelta = verdictBlocked.ScoreDelta, Blocked = true };
        var normalTurn = new Turn { SessionId = 1, Idx = 1, InstructorJson = JsonSerializer.Serialize(verdictNormal), ScoreDelta = verdictNormal.ScoreDelta, Blocked = false };
        context.Turns.AddRange(blockedTurn, normalTurn);
        // Simulate session score including only non-blocked delta
        session.ScoreTotal = verdictNormal.ScoreDelta;

        context.Metrics.Add(new Metric { SessionId = 1, K = "turn.normalized", V = verdictBlocked.Normalized });
        context.Metrics.Add(new Metric { SessionId = 1, K = "turn.normalized", V = verdictNormal.Normalized });
        await context.SaveChangesAsync();

        var stt = Mock.Of<ISttService>();
        var tts = Mock.Of<ITtsService>();
        var turnService = Mock.Of<PilotSim.Server.Services.ITurnService>();
        var hub = new Mock<IHubContext<PilotSim.Server.Hubs.LiveHub>>();
        var controller = new SimulationController(stt, tts, context, hub.Object, new NullLogger<SimulationController>(), turnService);

        var result = await controller.GetSessionSummaryAsync(1);
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var json = JsonSerializer.Serialize(ok.Value); // For quick inspection if needed

        // Deserialize back dynamic
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.Equal(2, root.GetProperty("totalTurns").GetInt32());
        Assert.Equal(1, root.GetProperty("blockedTurns").GetInt32());
        Assert.Equal(1, root.GetProperty("successfulTurns").GetInt32());
        // Average normalized (0.2 + 0.8)/2 = 0.5
        Assert.Equal(0.5, root.GetProperty("avgNormalized").GetDouble(), 3);
    }
}
