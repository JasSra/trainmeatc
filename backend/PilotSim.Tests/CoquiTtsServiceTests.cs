using Microsoft.Extensions.Logging;
using Moq;
using PilotSim.Server.Services;
using Xunit;
using Microsoft.AspNetCore.Hosting;

namespace PilotSim.Tests;

/// <summary>
/// End-to-end tests for Coqui TTS Service
/// Tests the complete text-to-speech pipeline including Python process invocation
/// </summary>
public class CoquiTtsServiceTests
{
    private readonly ILogger<CoquiTtsService> _logger;
    private readonly IWebHostEnvironment _environment;
    private readonly string _testOutputDir;

    public CoquiTtsServiceTests()
    {
        // Setup test environment
        _logger = new Mock<ILogger<CoquiTtsService>>().Object;
        
        var environmentMock = new Mock<IWebHostEnvironment>();
        _testOutputDir = Path.Combine(Path.GetTempPath(), "tts_test_output", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testOutputDir);
        
        environmentMock.Setup(e => e.WebRootPath).Returns(_testOutputDir);
        environmentMock.Setup(e => e.ContentRootPath).Returns(
            Path.GetDirectoryName(typeof(CoquiTtsService).Assembly.Location) ?? _testOutputDir);
        
        _environment = environmentMock.Object;
    }

    [Fact(Skip = "Requires Python and TTS library to be installed")]
    public async Task SynthesizeAsync_WithValidText_GeneratesAudioFile()
    {
        // Arrange
        var service = new CoquiTtsService(_logger, _environment);
        var testText = "Melbourne Tower, VH-ABC ready for departure runway one six right";

        // Act
        var result = await service.SynthesizeAsync(testText, "professional", "neutral", CancellationToken.None);

        // Assert
        Assert.NotEmpty(result);
        Assert.StartsWith("/audio/tts_", result);
        
        // Verify file exists
        var fileName = result.Replace("/audio/", "");
        var audioDir = Path.Combine(_testOutputDir, "audio");
        var filePath = Path.Combine(audioDir, fileName);
        Assert.True(File.Exists(filePath), $"Audio file should exist at {filePath}");
        
        // Verify file has content
        var fileInfo = new FileInfo(filePath);
        Assert.True(fileInfo.Length > 0, "Audio file should not be empty");
    }

    [Fact(Skip = "Requires Python and TTS library to be installed")]
    public async Task SynthesizeAsync_WithDifferentVoices_GeneratesAudioFiles()
    {
        // Arrange
        var service = new CoquiTtsService(_logger, _environment);
        var testText = "QFA456, cleared for takeoff runway two seven";
        var voices = new[] { "professional", "calm", "urgent", "default" };

        // Act & Assert
        foreach (var voice in voices)
        {
            var result = await service.SynthesizeAsync(testText, voice, "neutral", CancellationToken.None);
            
            Assert.NotEmpty(result);
            Assert.StartsWith("/audio/tts_", result);
            
            // Verify file exists
            var fileName = result.Replace("/audio/", "");
            var audioDir = Path.Combine(_testOutputDir, "audio");
            var filePath = Path.Combine(audioDir, fileName);
            Assert.True(File.Exists(filePath), $"Audio file should exist for voice '{voice}'");
        }
    }

    [Fact(Skip = "Requires Python and TTS library to be installed")]
    public async Task SynthesizeAsync_WithLongText_CompletesInReasonableTime()
    {
        // Arrange
        var service = new CoquiTtsService(_logger, _environment);
        var longText = "Melbourne Tower, VH-ABC, request taxi for departure. " +
                       "Ready at Alpha 3 with information Bravo. " +
                       "Melbourne Tower, VH-ABC, taxi to holding point Alpha 9 runway one six right via Alpha and Bravo. " +
                       "Hold short of runway two seven. QNH one zero one three.";

        // Act
        var startTime = DateTime.UtcNow;
        var result = await service.SynthesizeAsync(longText, "professional", "neutral", CancellationToken.None);
        var duration = DateTime.UtcNow - startTime;

        // Assert
        Assert.NotEmpty(result);
        // TTS should complete in under 30 seconds for this length
        Assert.True(duration.TotalSeconds < 30, $"TTS took {duration.TotalSeconds}s, should be < 30s");
    }

    [Fact]
    public async Task SynthesizeAsync_WithEmptyText_ReturnsEmpty()
    {
        // Arrange
        var service = new CoquiTtsService(_logger, _environment);

        // Act
        var result = await service.SynthesizeAsync("", "professional", "neutral", CancellationToken.None);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task SynthesizeAsync_WithNullText_ReturnsEmpty()
    {
        // Arrange
        var service = new CoquiTtsService(_logger, _environment);

        // Act
        var result = await service.SynthesizeAsync(null!, "professional", "neutral", CancellationToken.None);

        // Assert
        Assert.Empty(result);
    }

    [Fact(Skip = "Manual test - verifies Python script exists")]
    public void CoquiTtsService_PythonScriptExists()
    {
        // Arrange & Act
        var service = new CoquiTtsService(_logger, _environment);
        
        // The service should log if the script is missing
        // This is a construction test to verify the service can be created
        Assert.NotNull(service);
    }
}
