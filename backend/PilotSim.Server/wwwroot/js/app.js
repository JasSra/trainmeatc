// Modal management functions
window.showModal = (modalId) => {
    const modal = new bootstrap.Modal(document.getElementById(modalId));
    modal.show();
};

window.hideModal = (modalId) => {
    const modal = bootstrap.Modal.getInstance(document.getElementById(modalId));
    if (modal) {
        modal.hide();
    }
};

// Confirmation dialog
window.confirm = (message) => {
    return confirm(message);
};

// Audio playback for TTS
window.playAudio = (elementId, audioPath) => {
    const audio = document.getElementById(elementId);
    if (audio && audioPath) {
        audio.src = audioPath;
        audio.play().catch(error => {
            console.warn('Audio playback failed:', error);
        });
    }
};