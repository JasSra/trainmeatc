using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using PilotSim.Core;
using PilotSim.Data;
using PilotSim.Data.Models;
using PilotSim.Server.Controllers;
using PilotSim.Server.Hubs;
using Xunit;

namespace PilotSim.Tests;

public class UnitTest1
{
    [Fact]
    public void Placeholder()
    {
        Assert.True(true);
    }

    [Fact]
    public async Task SessionScoreEqualsSumOfNonBlockedTurns()
    {
        var options = new DbContextOptionsBuilder<SimDbContext>()
            .UseInMemoryDatabase(databaseName: "turn_score_test")
            .Options;

        await using var context = new SimDbContext(options);

        var scenario = new Scenario { Id = 1, Name = "Test", Difficulty = "Basic" };
        context.Scenarios.Add(scenario);
        var session = new Session { Id = 100, ScenarioId = scenario.Id, Difficulty = "Basic", StartedUtc = DateTime.UtcNow.ToString("O") };
        context.Sessions.Add(session);
        await context.SaveChangesAsync();

        // Fakes
        var stt = new Mock<ISttService>();
        stt.Setup(s => s.TranscribeAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SttResult("CALL SIGN TEST", new List<SttWord>()));

        var instructor = new Mock<IInstructorService>();
        // First turn blocked (ScoreDelta 0), second turn +5, third turn +3
        var sequence = new Queue<InstructorVerdict>(new[] {
            new InstructorVerdict(new List<string>{"Critical error"}, new List<string>(), null, 0.2, 0, "Block reason"),
            new InstructorVerdict(new List<string>(), new List<string>(), null, 0.8, 5, ""),
            new InstructorVerdict(new List<string>(), new List<string>(), null, 0.9, 3, "")
        });
        instructor.Setup(i => i.ScoreAsync(It.IsAny<string>(), It.IsAny<object>(), Difficulty.Basic, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => sequence.Dequeue());

        var atc = new Mock<IAtcService>();
        atc.Setup(a => a.NextAsync(It.IsAny<string>(), It.IsAny<object>(), Difficulty.Basic, It.IsAny<Load>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AtcReply("Roger", new List<string>{"Roger"}, new { state = 1 }, false, "professional"));

        var tts = new Mock<ITtsService>();
        tts.Setup(t => t.SynthesizeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("/audio/test.mp3");

        var hubClients = new Mock<IHubClients>();
    hubClients.Setup(c => c.Group(It.IsAny<string>())).Returns(new Mock<IClientProxy>().Object);
        var hubContext = new Mock<IHubContext<LiveHub>>();
        hubContext.SetupGet(h => h.Clients).Returns(hubClients.Object);

        var logger = new Mock<ILogger<SimulationController>>();
        var controller = new SimulationController(stt.Object, instructor.Object, atc.Object, tts.Object, context, hubContext.Object, logger.Object);

        async Task RunTurn()
        {
            var bytes = Encoding.UTF8.GetBytes("fake");
            var formFile = new FormFile(new MemoryStream(bytes), 0, bytes.Length, "Audio", "test.wav");
            var result = await controller.ProcessTurnAsync(new SimulationController.ProcessTurnRequest(session.Id, formFile, null), CancellationToken.None);
            Assert.NotNull(result);
        }

        await RunTurn(); // blocked
        await RunTurn(); // +5
        await RunTurn(); // +3

        var savedSession = await context.Sessions.Include(s => s.Turns).FirstAsync(s => s.Id == session.Id);
        Assert.Equal(8, savedSession.ScoreTotal); // 5 + 3
        Assert.Equal(3, savedSession.Turns.Count);
        Assert.True(savedSession.Turns.First().Blocked);
        Assert.Equal(0, savedSession.Turns.First().ScoreDelta);
    }
}
