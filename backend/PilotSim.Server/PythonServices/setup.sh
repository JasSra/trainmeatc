#!/bin/bash
# Setup script for Coqui TTS dependencies
# Run this once to install required Python packages for TTS

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
VENV_DIR="$SCRIPT_DIR/.venv"

echo "=== Coqui TTS Setup ==="
echo "This will install Python dependencies for text-to-speech"
echo ""

# Check if Python is available
if ! command -v python3 &> /dev/null; then
    echo "ERROR: Python 3 is not installed"
    echo "Please install Python 3.10 or later from https://www.python.org/"
    exit 1
fi

PYTHON_VERSION=$(python3 --version)
echo "Found: $PYTHON_VERSION"

# Create virtual environment if it doesn't exist
if [ ! -d "$VENV_DIR" ]; then
    echo "Creating virtual environment..."
    python3 -m venv "$VENV_DIR"
else
    echo "Virtual environment already exists"
fi

# Activate virtual environment
echo "Activating virtual environment..."
source "$VENV_DIR/bin/activate"

# Upgrade pip
echo "Upgrading pip..."
pip install --upgrade pip

# Install dependencies
echo "Installing Coqui TTS and dependencies..."
echo "This may take a few minutes on first run..."
pip install -r "$SCRIPT_DIR/requirements.txt"

echo ""
echo "=== Setup Complete ==="
echo "Coqui TTS is now ready to use!"
echo ""
echo "To use TTS in your ASP.NET application:"
echo "1. The application will automatically call Python with the virtual environment"
echo "2. Models will be downloaded automatically on first use (~150MB)"
echo "3. Subsequent runs will be faster as models are cached"
echo ""
echo "Virtual environment location: $VENV_DIR"
echo ""
