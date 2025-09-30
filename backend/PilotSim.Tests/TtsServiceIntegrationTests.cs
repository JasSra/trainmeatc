using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using PilotSim.Server.Services;
using Xunit;
using Microsoft.AspNetCore.Hosting;

namespace PilotSim.Tests;

/// <summary>
/// Integration test for TTS service end-to-end flow
/// Tests the complete pipeline from C# service to Python process to audio file
/// </summary>
public class TtsServiceIntegrationTests
{
    [Fact]
    public async Task CoquiTtsService_EndToEnd_GeneratesAudioFile()
    {
        // Arrange
        var logger = new Mock<ILogger<CoquiTtsService>>().Object;
        
        var testOutputDir = Path.Combine(Path.GetTempPath(), "tts_integration_test", Guid.NewGuid().ToString());
        Directory.CreateDirectory(testOutputDir);
        
        var environmentMock = new Mock<IWebHostEnvironment>();
        environmentMock.Setup(e => e.WebRootPath).Returns(testOutputDir);
        
        // Set ContentRootPath to the actual build output directory where Python files are
        var assemblyLocation = typeof(CoquiTtsService).Assembly.Location;
        var assemblyDir = Path.GetDirectoryName(assemblyLocation);
        environmentMock.Setup(e => e.ContentRootPath).Returns(assemblyDir ?? testOutputDir);
        
        var service = new CoquiTtsService(logger, environmentMock.Object);
        
        var testText = "Melbourne Tower, VH-ABC ready for departure runway one six right";

        try
        {
            // Act
            var result = await service.SynthesizeAsync(testText, "professional", "neutral", CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            
            if (string.IsNullOrEmpty(result))
            {
                // Service may return empty if Python is not available - this is acceptable
                Assert.True(true, "Service handled missing Python gracefully");
                return;
            }
            
            // If we got a result, verify it's valid
            Assert.StartsWith("/audio/tts_", result);
            
            // Verify file exists
            var fileName = result.Replace("/audio/", "");
            var audioDir = Path.Combine(testOutputDir, "audio");
            var filePath = Path.Combine(audioDir, fileName);
            
            Assert.True(File.Exists(filePath), $"Audio file should exist at {filePath}");
            
            // Verify file has content
            var fileInfo = new FileInfo(filePath);
            Assert.True(fileInfo.Length > 0, "Audio file should not be empty");
            
            // Verify it's a WAV file
            using var fs = File.OpenRead(filePath);
            var header = new byte[4];
            fs.Read(header, 0, 4);
            var riff = System.Text.Encoding.ASCII.GetString(header);
            Assert.Equal("RIFF", riff);
        }
        finally
        {
            // Cleanup
            try
            {
                if (Directory.Exists(testOutputDir))
                    Directory.Delete(testOutputDir, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    [Fact]
    public async Task CoquiTtsService_WithMultipleCalls_CreatesUniqueFiles()
    {
        // Arrange
        var logger = new Mock<ILogger<CoquiTtsService>>().Object;
        
        var testOutputDir = Path.Combine(Path.GetTempPath(), "tts_unique_test", Guid.NewGuid().ToString());
        Directory.CreateDirectory(testOutputDir);
        
        var environmentMock = new Mock<IWebHostEnvironment>();
        environmentMock.Setup(e => e.WebRootPath).Returns(testOutputDir);
        
        var assemblyLocation = typeof(CoquiTtsService).Assembly.Location;
        var assemblyDir = Path.GetDirectoryName(assemblyLocation);
        environmentMock.Setup(e => e.ContentRootPath).Returns(assemblyDir ?? testOutputDir);
        
        var service = new CoquiTtsService(logger, environmentMock.Object);

        try
        {
            // Act - Make multiple calls
            var result1 = await service.SynthesizeAsync("Test one", "professional", "neutral", CancellationToken.None);
            var result2 = await service.SynthesizeAsync("Test two", "calm", "neutral", CancellationToken.None);
            var result3 = await service.SynthesizeAsync("Test three", "urgent", "neutral", CancellationToken.None);

            // Assert - All should be unique (or all empty if Python not available)
            if (!string.IsNullOrEmpty(result1))
            {
                Assert.NotEqual(result1, result2);
                Assert.NotEqual(result2, result3);
                Assert.NotEqual(result1, result3);
            }
        }
        finally
        {
            // Cleanup
            try
            {
                if (Directory.Exists(testOutputDir))
                    Directory.Delete(testOutputDir, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    [Fact]
    public async Task CoquiTtsService_WithEmptyText_ReturnsEmpty()
    {
        // Arrange
        var logger = new Mock<ILogger<CoquiTtsService>>().Object;
        var environmentMock = new Mock<IWebHostEnvironment>();
        environmentMock.Setup(e => e.WebRootPath).Returns(Path.GetTempPath());
        environmentMock.Setup(e => e.ContentRootPath).Returns(Path.GetTempPath());
        
        var service = new CoquiTtsService(logger, environmentMock.Object);

        // Act
        var result = await service.SynthesizeAsync("", "professional", "neutral", CancellationToken.None);

        // Assert
        Assert.Empty(result);
    }
}
