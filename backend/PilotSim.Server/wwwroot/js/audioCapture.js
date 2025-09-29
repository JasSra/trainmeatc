// Audio capture functionality for TrainMeATC
let mediaRecorder = null;
let audioStream = null;
let audioChunks = [];
let audioContext = null;
let analyser = null;
let microphone = null;
let dataArray = null;
let animationId = null;

window.trainmeAudio = window.trainmeAudio || {};

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
        
        // Send to server to process the simulation turn
        await sendTurnAudio(audioBlob);
        
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
async function sendTurnAudio(audioBlob) {
    try {
        if (!window.currentSessionId) {
            throw new Error('Training session has not been started.');
        }

        // Create FormData to send audio file
        const formData = new FormData();
        formData.append('audio', audioBlob, 'recording.webm');
        formData.append('SessionId', window.currentSessionId);

        const response = await fetch('/api/simulation/turn', {
            method: 'POST',
            body: formData
        });
        
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        
        const result = await response.json();
        console.log('Turn result:', result);
        
        // Notify Blazor component of the result
        if (window.blazorTurnResultCallback) {
            await window.blazorTurnResultCallback(result);
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

// Play a short test tone to confirm speaker output
window.playTestTone = function() {
    try {
        const AudioContextConstructor = window.AudioContext || window.webkitAudioContext;
        if (!AudioContextConstructor) {
            throw new Error('Web Audio API is not supported in this browser.');
        }

        const context = new AudioContextConstructor();
        const oscillator = context.createOscillator();
        const gainNode = context.createGain();

        oscillator.type = 'sine';
        oscillator.frequency.value = 880; // A5 tone
        gainNode.gain.value = 0.15;

        oscillator.connect(gainNode);
        gainNode.connect(context.destination);

        oscillator.start();
        oscillator.stop(context.currentTime + 1.0);
        oscillator.onended = () => {
            gainNode.disconnect();
            context.close();
        };
    } catch (error) {
        console.error('Error playing test tone:', error);
        throw error;
    }
};

window.trainmeAudio.testMicrophone = async function() {
    if (!(navigator.mediaDevices && navigator.mediaDevices.getUserMedia)) {
        return { success: false, message: 'Browser does not support microphone capture.' };
    }

    let testStream;
    try {
        testStream = await navigator.mediaDevices.getUserMedia({
            audio: {
                sampleRate: 16000,
                channelCount: 1,
                echoCancellation: true,
                noiseSuppression: true,
                autoGainControl: true
            }
        });

        const devices = await navigator.mediaDevices.enumerateDevices();
        const inputCount = devices.filter(device => device.kind === 'audioinput').length;

        return {
            success: true,
            message: inputCount > 0
                ? `Detected ${inputCount} audio input${inputCount === 1 ? '' : 's'}.`
                : 'Microphone access granted.'
        };
    } catch (error) {
        console.error('Microphone test failed:', error);
        let message = 'Unable to access microphone.';
        if (error && (error.name === 'NotAllowedError' || error.name === 'SecurityError')) {
            message = 'Microphone permission denied. Please enable access and try again.';
        }
        return { success: false, message };
    } finally {
        if (testStream) {
            testStream.getTracks().forEach(track => track.stop());
        }
    }
};

window.trainmeAudio.testSpeakers = async function() {
    try {
        window.playTestTone();
        return { success: true, message: 'Tone played. Adjust your volume if you did not hear it.' };
    } catch (error) {
        console.error('Speaker test failed:', error);
        return { success: false, message: 'Unable to play test tone. Check your output device.' };
    }
};

console.log('Audio capture module loaded');
