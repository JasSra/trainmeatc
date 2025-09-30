using System.Diagnostics;
using System.Text;
using System.Text.Json;
using PilotSim.Core;

namespace PilotSim.Server.Services;

/// <summary>
/// Coqui TTS Service - Uses local Coqui AI TTS via Python process
/// No OpenAI dependency - fast local text-to-speech synthesis
/// </summary>
public class CoquiTtsService : ITtsService
{
    private readonly ILogger<CoquiTtsService> _logger;
    private readonly IWebHostEnvironment _environment;
    private readonly string _audioDirectory;
    private readonly string _pythonScriptPath;
    private readonly string _pythonExecutable;
    private static readonly SemaphoreSlim _pythonLock = new(1, 1);

    public CoquiTtsService(ILogger<CoquiTtsService> logger, IWebHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
        _audioDirectory = Path.Combine(environment.WebRootPath, "audio");
        Directory.CreateDirectory(_audioDirectory);

        // Locate Python script
        var serverDir = Path.GetDirectoryName(typeof(CoquiTtsService).Assembly.Location) ?? environment.ContentRootPath;
        _pythonScriptPath = Path.Combine(serverDir, "PythonServices", "coqui_tts_service.py");
        
        // Try to find Python executable
        _pythonExecutable = FindPythonExecutable();
        
        _logger.LogInformation("CoquiTtsService initialized. Python: {Python}, Script: {Script}", 
            _pythonExecutable, _pythonScriptPath);
    }

    private string FindPythonExecutable()
    {
        // First, check if there's a virtual environment in the PythonServices directory
        var pythonServicesDir = Path.GetDirectoryName(_pythonScriptPath);
        if (!string.IsNullOrEmpty(pythonServicesDir))
        {
            var venvPaths = new[]
            {
                Path.Combine(pythonServicesDir, ".venv", "bin", "python"),      // Linux/Mac
                Path.Combine(pythonServicesDir, ".venv", "Scripts", "python.exe") // Windows
            };

            foreach (var venvPath in venvPaths)
            {
                if (File.Exists(venvPath))
                {
                    _logger.LogInformation("Using Python from virtual environment: {Path}", venvPath);
                    return venvPath;
                }
            }
        }

        // Fall back to system Python
        var pythonNames = new[] { "python3", "python", "python3.12", "python3.11", "python3.10" };
        
        foreach (var name in pythonNames)
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = name,
                        Arguments = "--version",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                
                process.Start();
                process.WaitForExit(2000);
                
                if (process.ExitCode == 0)
                {
                    _logger.LogInformation("Found Python executable: {Name}", name);
                    return name;
                }
            }
            catch
            {
                // Try next option
            }
        }
        
        _logger.LogWarning("Python executable not found, defaulting to 'python3'");
        return "python3";
    }

    public async Task<string> SynthesizeAsync(string text, string voice, string style, CancellationToken cancellationToken)
    {
        try
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(text))
            {
                _logger.LogWarning("Empty text provided for synthesis");
                return "";
            }

            // Generate unique filename
            var fileName = $"tts_{Guid.NewGuid():N}.wav";
            var filePath = Path.Combine(_audioDirectory, fileName);

            // Check if Python script exists
            if (!File.Exists(_pythonScriptPath))
            {
                _logger.LogError("Python script not found at: {Path}", _pythonScriptPath);
                return "";
            }

            // Call Python TTS service
            var success = await RunPythonTtsAsync(text, filePath, voice, style, cancellationToken);

            if (!success || !File.Exists(filePath))
            {
                _logger.LogError("TTS synthesis failed or output file not created");
                return "";
            }

            _logger.LogInformation("TTS synthesis successful: {FileName}, Size: {Size} bytes", 
                fileName, new FileInfo(filePath).Length);

            return $"/audio/{fileName}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to synthesize speech for text: {Text}", text);
            return "";
        }
    }

    private async Task<bool> RunPythonTtsAsync(string text, string outputPath, string voice, string style, CancellationToken cancellationToken)
    {
        // Only allow one Python process at a time to avoid resource contention
        await _pythonLock.WaitAsync(cancellationToken);
        
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = _pythonExecutable,
                Arguments = $"\"{_pythonScriptPath}\" --text \"{EscapeArgument(text)}\" --output \"{EscapeArgument(outputPath)}\" --voice \"{voice}\" --style \"{style}\" --json",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetDirectoryName(_pythonScriptPath)
            };

            using var process = new Process { StartInfo = startInfo };
            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();

            process.OutputDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                    outputBuilder.AppendLine(e.Data);
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                    errorBuilder.AppendLine(e.Data);
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            // Wait for process with timeout (30 seconds should be enough for TTS)
            var timeout = TimeSpan.FromSeconds(30);
            var completed = await Task.Run(() => process.WaitForExit((int)timeout.TotalMilliseconds), cancellationToken);

            if (!completed)
            {
                _logger.LogError("Python TTS process timed out after {Timeout}s", timeout.TotalSeconds);
                try { process.Kill(); } catch { }
                return false;
            }

            var output = outputBuilder.ToString();
            var error = errorBuilder.ToString();

            if (process.ExitCode != 0)
            {
                _logger.LogError("Python TTS failed with exit code {ExitCode}. Error: {Error}", 
                    process.ExitCode, error);
                return false;
            }

            // Try to parse JSON output
            try
            {
                if (!string.IsNullOrWhiteSpace(output))
                {
                    var result = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(output.Trim());
                    if (result != null && result.TryGetValue("status", out var status))
                    {
                        var statusStr = status.GetString();
                        if (statusStr == "success")
                        {
                            _logger.LogDebug("Python TTS succeeded: {Output}", output);
                            return true;
                        }
                        else
                        {
                            _logger.LogWarning("Python TTS returned non-success status: {Output}", output);
                        }
                    }
                }
            }
            catch (JsonException)
            {
                // If JSON parsing fails, check if file was created as fallback
                _logger.LogWarning("Could not parse Python TTS JSON output: {Output}", output);
            }

            // Fallback: check if output file exists
            return File.Exists(outputPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running Python TTS process");
            return false;
        }
        finally
        {
            _pythonLock.Release();
        }
    }

    private static string EscapeArgument(string arg)
    {
        // Escape quotes and backslashes for command line
        return arg.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
