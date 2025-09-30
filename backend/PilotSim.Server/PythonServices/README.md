# Coqui TTS Service

This directory contains the Coqui AI TTS (Text-to-Speech) service integration for the PilotSim application.

## Overview

The Coqui TTS service provides **fast, local text-to-speech synthesis** without requiring external API calls or API keys. It uses the open-source [Coqui AI TTS library](https://github.com/coqui-ai/TTS) to generate high-quality speech from text.

## Features

- ✅ **No API Keys Required** - Runs completely locally
- ✅ **Fast Synthesis** - Optimized for real-time speech generation
- ✅ **Self-Contained** - Models auto-download on first use
- ✅ **High Quality** - Natural-sounding speech output
- ✅ **Multiple Voices** - Support for different voice styles

## Prerequisites

- **Python 3.10 or later** - [Download Python](https://www.python.org/downloads/)
- **~500MB disk space** - For Python packages and TTS models

## Installation

### Option 1: Automatic Setup (Recommended)

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

### Option 2: Manual Setup

1. Create a virtual environment:
   ```bash
   python3 -m venv .venv
   source .venv/bin/activate  # On Windows: .venv\Scripts\activate
   ```

2. Install dependencies:
   ```bash
   pip install -r requirements.txt
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

## Performance

- **First synthesis**: ~5-10 seconds (includes model download)
- **Subsequent syntheses**: ~1-3 seconds per sentence
- **File size**: ~50KB per second of audio (WAV format)

## Architecture

```
ASP.NET Application (C#)
    ↓
CoquiTtsService.cs
    ↓ (Process invocation)
coqui_tts_service.py
    ↓
Coqui TTS Library
    ↓
Audio File (WAV)
```

## Troubleshooting

### "TTS library not installed" error

Run the setup script to install dependencies:
```bash
./setup.sh  # or setup.bat on Windows
```

### "Python executable not found" error

Ensure Python 3.10+ is installed and in your system PATH:
```bash
python3 --version
```

### Slow synthesis

- First run downloads models (~150MB) - this is normal
- Ensure you have a fast disk (SSD recommended)
- Consider using a GPU for faster inference (optional)

### Models not downloading

Check your internet connection. Models are downloaded from Hugging Face on first use.

## Development

### Running Tests

```bash
cd backend
dotnet test --filter "FullyQualifiedName~CoquiTtsServiceTests"
```

Note: Some tests require Python and TTS to be installed and are marked with `[Fact(Skip = "...")]`.

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

## Comparison with OpenAI TTS

| Feature | Coqui TTS | OpenAI TTS |
|---------|-----------|------------|
| Cost | Free | Pay per character |
| API Key | Not required | Required |
| Latency | 1-3s local | 2-5s network |
| Quality | High | Very high |
| Voices | Limited | Many options |
| Privacy | Full privacy | Sends to API |

## License

Coqui TTS is licensed under the MPL 2.0 license. See [Coqui AI TTS](https://github.com/coqui-ai/TTS) for details.

## Support

For issues with:
- **TTS Integration**: Open an issue in this repository
- **Coqui TTS Library**: See [Coqui AI TTS](https://github.com/coqui-ai/TTS)
