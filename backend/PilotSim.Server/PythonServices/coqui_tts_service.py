#!/usr/bin/env python3
"""
TTS Service - Text-to-speech service
Supports multiple TTS backends: gTTS, Coqui TTS
Falls back to a simple beep generation for testing without dependencies
"""

import sys
import os
import argparse
import json
from pathlib import Path

def check_tts_availability():
    """Check which TTS library is available"""
    # Priority: gTTS (simplest), then Coqui TTS
    try:
        import gtts
        return 'gtts'
    except ImportError:
        pass
    
    try:
        import TTS
        return 'coqui'
    except ImportError:
        pass
    
    # Check if we can create a simple WAV file as fallback
    try:
        import wave
        import struct
        return 'mock'
    except ImportError:
        pass
    
    return None

def synthesize_with_gtts(text: str, output_path: str):
    """Synthesize using gTTS (simple, widely compatible)"""
    from gtts import gTTS
    
    # Create TTS object
    tts = gTTS(text=text, lang='en', slow=False)
    
    # Save to file
    tts.save(output_path)
    
    return {
        "status": "success",
        "output_path": output_path,
        "message": "Speech synthesized successfully using gTTS"
    }

def synthesize_with_coqui(text: str, output_path: str):
    """Synthesize using Coqui TTS (higher quality, offline)"""
    from TTS.api import TTS
    
    # Initialize TTS with a fast model
    model_name = "tts_models/en/ljspeech/fast_pitch"
    
    # Create TTS instance (will download model on first run)
    tts = TTS(model_name=model_name, progress_bar=False)
    
    # Synthesize speech
    tts.tts_to_file(text=text, file_path=output_path)
    
    return {
        "status": "success",
        "output_path": output_path,
        "message": f"Speech synthesized successfully using {model_name}"
    }

def synthesize_with_mock(text: str, output_path: str):
    """Create a simple WAV file for testing (no actual speech)"""
    import wave
    import struct
    import math
    
    # Create a simple beep pattern based on text length
    sample_rate = 22050
    duration = min(len(text) * 0.05, 5.0)  # ~50ms per character, max 5 seconds
    num_samples = int(sample_rate * duration)
    
    # Generate a simple tone
    frequency = 440  # A4 note
    
    with wave.open(output_path, 'w') as wav_file:
        # Set parameters (1 channel, 2 bytes per sample, sample rate)
        wav_file.setnchannels(1)
        wav_file.setsampwidth(2)
        wav_file.setframerate(sample_rate)
        
        # Generate tone
        for i in range(num_samples):
            # Simple sine wave with envelope
            t = i / sample_rate
            envelope = 0.5 * (1 - abs(2*t/duration - 1))  # Triangle envelope
            value = int(32767 * envelope * math.sin(2 * math.pi * frequency * t))
            wav_file.writeframes(struct.pack('h', value))
    
    return {
        "status": "success",
        "output_path": output_path,
        "message": f"Mock audio generated for testing (duration: {duration:.1f}s). Install gTTS or TTS for actual speech synthesis."
    }

def ensure_tts_installed():
    """Ensure a TTS library is available"""
    tts_type = check_tts_availability()
    if tts_type is None:
        print("ERROR: No TTS library or fallback available.", file=sys.stderr)
        print("Please install one of:", file=sys.stderr)
        print("  pip install gTTS       # Simple, recommended (requires internet)", file=sys.stderr)
        print("  pip install TTS        # Higher quality, offline (requires Python 3.9-3.11)", file=sys.stderr)
        return False
    return True

def synthesize_speech(text: str, output_path: str, voice: str = "default", style: str = "neutral"):
    """
    Synthesize speech from text using available TTS library
    
    Args:
        text: Text to synthesize
        output_path: Path where to save the audio file
        voice: Voice selection (professional, calm, urgent, default)
        style: Style/tone of voice (not used currently but kept for API compatibility)
    
    Returns:
        dict: Result with status and output path
    """
    try:
        tts_type = check_tts_availability()
        
        if tts_type == 'gtts':
            return synthesize_with_gtts(text, output_path)
        elif tts_type == 'coqui':
            return synthesize_with_coqui(text, output_path)
        elif tts_type == 'mock':
            return synthesize_with_mock(text, output_path)
        else:
            return {
                "status": "error",
                "error": "No TTS library available",
                "message": "Please install gTTS or TTS"
            }
        
    except Exception as e:
        # Fallback to mock if real TTS fails
        import traceback
        print(f"TTS failed, falling back to mock: {e}", file=sys.stderr)
        print(traceback.format_exc(), file=sys.stderr)
        try:
            return synthesize_with_mock(text, output_path)
        except Exception as e2:
            return {
                "status": "error",
                "error": str(e),
                "message": f"Failed to synthesize speech: {e}"
            }

def main():
    parser = argparse.ArgumentParser(description="TTS Service")
    parser.add_argument("--text", required=True, help="Text to synthesize")
    parser.add_argument("--output", required=True, help="Output audio file path")
    parser.add_argument("--voice", default="default", help="Voice selection")
    parser.add_argument("--style", default="neutral", help="Voice style")
    parser.add_argument("--json", action="store_true", help="Output JSON response")
    
    args = parser.parse_args()
    
    # Ensure TTS is installed
    if not ensure_tts_installed():
        if args.json:
            print(json.dumps({
                "status": "error",
                "error": "TTS library not installed"
            }))
        sys.exit(1)
    
    # Ensure output directory exists
    output_dir = os.path.dirname(args.output)
    if output_dir:
        os.makedirs(output_dir, exist_ok=True)
    
    # Synthesize speech
    result = synthesize_speech(args.text, args.output, args.voice, args.style)
    
    # Output result
    if args.json:
        print(json.dumps(result))
    else:
        if result["status"] == "success":
            print(f"Success: {result['message']}")
            print(f"Output: {result['output_path']}")
        else:
            print(f"Error: {result['message']}", file=sys.stderr)
            sys.exit(1)
    
    sys.exit(0 if result["status"] == "success" else 1)

if __name__ == "__main__":
    main()
