/**
 * Audio Share Functionality
 * Uses Web Share API for sharing synthesized audio on mobile devices
 */

window.audioShare = {
    /**
     * Check if Web Share API is supported
     * @returns {boolean} True if sharing is supported
     */
    isShareSupported() {
        return navigator.share && navigator.canShare;
    },

    /**
     * Share audio file
     * @param {string} base64Audio - Base64 encoded audio data
     * @param {string} mimeType - MIME type (e.g., 'audio/mpeg', 'audio/wav')
     * @param {string} filename - Suggested filename (e.g., 'speech.mp3')
     * @param {string} text - Optional text to share with the audio
     * @returns {Promise<boolean>} Success status
     */
    async shareAudio(base64Audio, mimeType, filename, text) {
        try {
            if (!this.isShareSupported()) {
                console.warn('Web Share API not supported on this device');
                return false;
            }

            // Convert base64 to Blob
            const byteCharacters = atob(base64Audio);
            const byteNumbers = new Array(byteCharacters.length);
            for (let i = 0; i < byteCharacters.length; i++) {
                byteNumbers[i] = byteCharacters.charCodeAt(i);
            }
            const byteArray = new Uint8Array(byteNumbers);
            const blob = new Blob([byteArray], { type: mimeType });

            // Create File object from Blob
            const file = new File([blob], filename, { type: mimeType });

            // Prepare share data
            const shareData = {
                files: [file],
                title: 'Synthesized Speech',
                text: text || 'Listen to this synthesized speech'
            };

            // Check if the data can be shared
            if (navigator.canShare && !navigator.canShare(shareData)) {
                console.warn('Cannot share this data');
                return false;
            }

            // Share
            await navigator.share(shareData);
            return true;
        } catch (error) {
            if (error.name === 'AbortError') {
                // User cancelled the share dialog
                console.log('Share cancelled by user');
                return false;
            }
            console.error('Error sharing audio:', error.message);
            return false;
        }
    }
};
