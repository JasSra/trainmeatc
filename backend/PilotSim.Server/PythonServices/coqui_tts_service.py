#!/usr/bin/env python3
"""
Coqui TTS Service - Self-contained text-to-speech service
Uses Coqui AI TTS library for fast, local speech synthesis
No external API calls or installation required - models are auto-downloaded
"""

import sys
import os
import argparse
import json
from pathlib import Path

def ensure_tts_installed():
    """Ensure TTS library is available"""
    try:
        import TTS
        return True
    except ImportError:
        print("ERROR: TTS library not installed. Please run: pip install TTS", file=sys.stderr)
        return False

def synthesize_speech(text: str, output_path: str, voice: str = "default", style: str = "neutral"):
    """
    Synthesize speech from text using Coqui TTS
    
    Args:
        text: Text to synthesize
        output_path: Path where to save the audio file
        voice: Voice selection (professional, calm, urgent, default)
        style: Style/tone of voice (not used currently but kept for API compatibility)
    
    Returns:
        dict: Result with status and output path
    """
    try:
        from TTS.api import TTS
        
        # Initialize TTS with a fast, high-quality model
        # Using "tts_models/en/ljspeech/tacotron2-DDC" - fast and good quality
        # Alternative: "tts_models/en/ljspeech/fast_pitch" for even faster synthesis
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
        
    except Exception as e:
        return {
            "status": "error",
            "error": str(e),
            "message": f"Failed to synthesize speech: {e}"
        }

def main():
    parser = argparse.ArgumentParser(description="Coqui TTS Service")
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
