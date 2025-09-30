# TTS Service

This directory contains the TTS (Text-to-Speech) service integration for the PilotSim application.

## Overview

The TTS service provides **fast, local text-to-speech synthesis** without requiring external API calls or API keys (when using Coqui TTS). It supports multiple TTS backends:

1. **gTTS** (Google Text-to-Speech) - Simple, widely compatible (requires internet)
2. **Coqui AI TTS** - High quality, offline (requires Python 3.9-3.11)
3. **Mock fallback** - Simple audio generation for testing without dependencies

## Features

- ✅ **No API Keys Required** - Runs completely locally (with Coqui TTS)
- ✅ **Fast Synthesis** - Optimized for real-time speech generation
- ✅ **Self-Contained** - Models auto-download on first use (Coqui TTS)
- ✅ **Multiple Backends** - Flexible deployment options
- ✅ **Fallback Support** - Works even without TTS libraries installed

## Prerequisites

- **Python 3.10 or later** - [Download Python](https://www.python.org/downloads/)
- **~500MB disk space** - For Python packages and TTS models (if using Coqui)

## Installation

### Quick Start (gTTS - Requires Internet)

```bash
cd backend/PilotSim.Server/PythonServices
pip install gTTS
```

### For Offline Use (Coqui TTS - Python 3.9-3.11 only)

```bash
cd backend/PilotSim.Server/PythonServices
# Only works with Python 3.9, 3.10, or 3.11
pip install TTS
```

### Automatic Setup Script

#### On Linux/Mac:
```bash
cd backend/PilotSim.Server/PythonServices
./setup.sh
```

#### On Windows:
```cmd
cd backend\PilotSim.Server\PythonServices
setup.bat
```

## Usage

The TTS service is automatically integrated into the ASP.NET application. No additional configuration is needed.

### Testing the Service

You can test the TTS service directly from the command line:

```bash
python3 coqui_tts_service.py \
  --text "Melbourne Tower, VH-ABC ready for departure" \
  --output test_output.wav \
  --voice professional \
  --json
```

### API Usage

The service is exposed through the `/api/tts` endpoint:

```http
POST /api/tts
Content-Type: application/json

{
  "text": "Melbourne Tower, VH-ABC ready for departure runway one six right",
  "voice": "professional",
  "style": "neutral"
}
```

Response:
```json
{
  "audioPath": "/audio/tts_abc123.wav"
}
```

## Voice Options

- `professional` - Clear, authoritative voice (default)
- `calm` - Relaxed, steady voice
- `urgent` - Higher energy voice
- `default` - Standard voice

*Note: Voice selection is currently limited with gTTS but fully supported with Coqui TTS*

## Performance

### gTTS (Internet Required)
- **First synthesis**: ~1-3 seconds
- **Subsequent syntheses**: ~1-3 seconds per sentence
- **File size**: ~30KB per second of audio (MP3 format)

### Coqui TTS (Offline)
- **First synthesis**: ~5-10 seconds (includes model download)
- **Subsequent syntheses**: ~1-3 seconds per sentence
- **File size**: ~50KB per second of audio (WAV format)

### Mock Fallback
- **Synthesis**: <100ms
- **File size**: ~40KB per second of audio (WAV format)
- *Note: Generates simple tone, not actual speech*

## Architecture

```
ASP.NET Application (C#)
    ↓
CoquiTtsService.cs
    ↓ (Process invocation)
coqui_tts_service.py
    ↓
TTS Library (gTTS or Coqui)
    ↓
Audio File (WAV/MP3)
```

## Troubleshooting

### "TTS library not installed" error

Run the setup script or install manually:
```bash
pip install gTTS  # Simple option
# OR
pip install TTS   # Offline option (Python 3.9-3.11 only)
```

### "Python executable not found" error

Ensure Python 3.10+ is installed and in your system PATH:
```bash
python3 --version
```

### Slow synthesis

- **gTTS**: Check internet connection
- **Coqui TTS**: First run downloads models (~150MB) - this is normal
- Consider using a GPU for faster Coqui TTS inference (optional)

### "Failed to connect" with gTTS

gTTS requires internet access. For offline use, install Coqui TTS instead (Python 3.9-3.11 only).

### Mock audio generated instead of speech

This happens when no TTS library is installed. Install gTTS or Coqui TTS for actual speech synthesis.

## Development

### Running Tests

```bash
cd backend
dotnet test --filter "FullyQualifiedName~TtsServiceIntegrationTests"
```

### Debugging

Enable verbose logging in `appsettings.Development.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "PilotSim.Server.Services.CoquiTtsService": "Debug"
    }
  }
}
```

## Files

- `coqui_tts_service.py` - Python TTS service script
- `requirements.txt` - Python dependencies
- `setup.sh` - Linux/Mac setup script
- `setup.bat` - Windows setup script
- `README.md` - This file

## Comparison of TTS Options

| Feature | gTTS | Coqui TTS | OpenAI TTS |
|---------|------|-----------|------------|
| Cost | Free | Free | Pay per character |
| API Key | Not required | Not required | Required |
| Latency | 1-3s network | 1-3s local | 2-5s network |
| Quality | Good | High | Very high |
| Voices | Limited | Multiple | Many options |
| Privacy | Sends to Google | Full privacy | Sends to API |
| Offline | No | Yes | No |
| Python Version | Any | 3.9-3.11 | N/A |

## License

- **gTTS**: MIT License
- **Coqui TTS**: MPL 2.0 License. See [Coqui AI TTS](https://github.com/coqui-ai/TTS) for details.

## Support

For issues with:
- **TTS Integration**: Open an issue in this repository
- **gTTS Library**: See [gTTS Documentation](https://gtts.readthedocs.io/)
- **Coqui TTS Library**: See [Coqui AI TTS](https://github.com/coqui-ai/TTS)
