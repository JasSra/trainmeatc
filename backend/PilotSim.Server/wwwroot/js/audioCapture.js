// Audio capture functionality for TrainMeATC
let mediaRecorder = null;
let audioStream = null;
let audioChunks = [];
let audioContext = null;
let analyser = null;
let microphone = null;
let dataArray = null;
let animationId = null;

// Initialize audio capture
window.startAudioCapture = async function() {
    try {
        // Request microphone access
        audioStream = await navigator.mediaDevices.getUserMedia({
            audio: {
                sampleRate: 16000,
                channelCount: 1,
                echoCancellation: true,
                noiseSuppression: true,
                autoGainControl: true
            }
        });
        
        // Set up audio context for level monitoring
        audioContext = new (window.AudioContext || window.webkitAudioContext)();
        analyser = audioContext.createAnalyser();
        microphone = audioContext.createMediaStreamSource(audioStream);
        
        analyser.fftSize = 256;
        const bufferLength = analyser.frequencyBinCount;
        dataArray = new Uint8Array(bufferLength);
        
        microphone.connect(analyser);
        
        // Start level monitoring
        monitorAudioLevel();
        
        // Set up MediaRecorder for audio capture
        mediaRecorder = new MediaRecorder(audioStream, {
            mimeType: 'audio/webm;codecs=opus'
        });
        
        audioChunks = [];
        
        mediaRecorder.ondataavailable = function(event) {
            if (event.data.size > 0) {
                audioChunks.push(event.data);
            }
        };
        
        mediaRecorder.onstop = function() {
            processAudioChunks();
        };
        
        // Start recording
        mediaRecorder.start(100); // Collect data every 100ms
        
        console.log('Audio capture started');
        
    } catch (error) {
        console.error('Error starting audio capture:', error);
        alert('Could not access microphone. Please check your permissions.');
    }
};

// Stop audio capture
window.stopAudioCapture = function() {
    try {
        if (mediaRecorder && mediaRecorder.state !== 'inactive') {
            mediaRecorder.stop();
        }
        
        if (audioStream) {
            audioStream.getTracks().forEach(track => track.stop());
            audioStream = null;
        }
        
        if (audioContext) {
            audioContext.close();
            audioContext = null;
        }
        
        if (animationId) {
            cancelAnimationFrame(animationId);
            animationId = null;
        }
        
        console.log('Audio capture stopped');
        
    } catch (error) {
        console.error('Error stopping audio capture:', error);
    }
};

// Monitor audio level for visual feedback
function monitorAudioLevel() {
    if (!analyser || !dataArray) return;
    
    analyser.getByteFrequencyData(dataArray);
    
    // Calculate RMS level
    let sum = 0;
    for (let i = 0; i < dataArray.length; i++) {
        sum += dataArray[i] * dataArray[i];
    }
    const rms = Math.sqrt(sum / dataArray.length);
    const level = rms / 255; // Normalize to 0-1
    
    // Update level indicator via Blazor callback
    if (window.blazorAudioLevelCallback) {
        window.blazorAudioLevelCallback(level);
    }
    
    // Continue monitoring
    animationId = requestAnimationFrame(monitorAudioLevel);
}

// Process recorded audio chunks
async function processAudioChunks() {
    if (audioChunks.length === 0) {
        console.log('No audio data to process');
        return;
    }
    
    try {
        // Create blob from recorded chunks
        const audioBlob = new Blob(audioChunks, { type: 'audio/webm;codecs=opus' });
        
        // Send to server for STT processing
        await sendAudioToServer(audioBlob);
        
    } catch (error) {
        console.error('Error processing audio:', error);
    }
}

// Convert blob to base64
function blobToBase64(blob) {
    return new Promise((resolve, reject) => {
        const reader = new FileReader();
        reader.onload = () => {
            const base64 = reader.result.split(',')[1]; // Remove data URL prefix
            resolve(base64);
        };
        reader.onerror = reject;
        reader.readAsDataURL(blob);
    });
}

// Send audio to server for processing
async function sendAudioToServer(audioBlob) {
    try {
        // Create FormData to send audio file
        const formData = new FormData();
        formData.append('audio', audioBlob, 'recording.webm');
        formData.append('biasPrompt', 'aviation communication pilot ATC radio');
        
        const response = await fetch('/api/stt', {
            method: 'POST',
            body: formData
        });
        
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        
        const result = await response.json();
        console.log('STT result:', result);
        
        // Notify Blazor component of the result
        if (window.blazorSttCallback && result.text) {
            await window.blazorSttCallback(result.text, result.confidence || 0.0);
        }
        
        return result;
        
    } catch (error) {
        console.error('Error sending audio to server:', error);
        
        // Notify Blazor of the error
        if (window.blazorSttErrorCallback) {
            await window.blazorSttErrorCallback(error.message);
        }
        
        throw error;
    }
}

// Play audio file
window.playAudio = function(audioElementId, audioPath) {
    try {
        const audioElement = document.getElementById(audioElementId);
        if (audioElement) {
            audioElement.src = audioPath;
            audioElement.play();
        }
    } catch (error) {
        console.error('Error playing audio:', error);
    }
};

// Check if audio capture is supported
window.isAudioCaptureSupported = function() {
    return !!(navigator.mediaDevices && navigator.mediaDevices.getUserMedia);
};

// Get audio devices
window.getAudioDevices = async function() {
    try {
        const devices = await navigator.mediaDevices.enumerateDevices();
        return devices.filter(device => device.kind === 'audioinput');
    } catch (error) {
        console.error('Error getting audio devices:', error);
        return [];
    }
};

console.log('Audio capture module loaded');