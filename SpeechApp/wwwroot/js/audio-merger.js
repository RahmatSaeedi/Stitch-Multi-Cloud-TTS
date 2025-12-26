// Audio merging using Web Audio API

window.audioMerger = {
    // Merge multiple audio chunks into a single file
    async mergeAudioChunks(base64Chunks) {
        try {
            const audioContext = new (window.AudioContext || window.webkitAudioContext)();
            const buffers = [];

            // Decode all chunks
            for (const base64 of base64Chunks) {
                const binaryString = atob(base64);
                const bytes = new Uint8Array(binaryString.length);
                for (let i = 0; i < binaryString.length; i++) {
                    bytes[i] = binaryString.charCodeAt(i);
                }

                const audioBuffer = await audioContext.decodeAudioData(bytes.buffer);
                buffers.push(audioBuffer);
            }

            // Calculate total length
            const totalLength = buffers.reduce((sum, buffer) => sum + buffer.length, 0);
            const sampleRate = buffers[0].sampleRate;
            const numberOfChannels = buffers[0].numberOfChannels;

            // Create merged buffer
            const mergedBuffer = audioContext.createBuffer(
                numberOfChannels,
                totalLength,
                sampleRate
            );

            // Copy data from all buffers
            let offset = 0;
            for (const buffer of buffers) {
                for (let channel = 0; channel < numberOfChannels; channel++) {
                    const channelData = buffer.getChannelData(channel);
                    mergedBuffer.copyToChannel(channelData, channel, offset);
                }
                offset += buffer.length;
            }

            // Convert to WAV (since we can't directly export MP3 in browser without additional libraries)
            const wavBlob = await this.bufferToWave(mergedBuffer);
            const arrayBuffer = await wavBlob.arrayBuffer();
            const bytes = new Uint8Array(arrayBuffer);

            // Convert to base64
            let binary = '';
            for (let i = 0; i < bytes.byteLength; i++) {
                binary += String.fromCharCode(bytes[i]);
            }

            await audioContext.close();

            return btoa(binary);
        } catch (error) {
            console.error('Audio merging error:', error);
            throw error;
        }
    },

    // Convert AudioBuffer to WAV blob
    async bufferToWave(audioBuffer) {
        const numberOfChannels = audioBuffer.numberOfChannels;
        const sampleRate = audioBuffer.sampleRate;
        const format = 1; // PCM
        const bitDepth = 16;

        let result;
        if (numberOfChannels === 2) {
            result = this.interleave(
                audioBuffer.getChannelData(0),
                audioBuffer.getChannelData(1)
            );
        } else {
            result = audioBuffer.getChannelData(0);
        }

        const buffer = new ArrayBuffer(44 + result.length * 2);
        const view = new DataView(buffer);

        // RIFF identifier
        this.writeString(view, 0, 'RIFF');
        // file length
        view.setUint32(4, 36 + result.length * 2, true);
        // RIFF type
        this.writeString(view, 8, 'WAVE');
        // format chunk identifier
        this.writeString(view, 12, 'fmt ');
        // format chunk length
        view.setUint32(16, 16, true);
        // sample format (raw)
        view.setUint16(20, format, true);
        // channel count
        view.setUint16(22, numberOfChannels, true);
        // sample rate
        view.setUint32(24, sampleRate, true);
        // byte rate (sample rate * block align)
        view.setUint32(28, sampleRate * numberOfChannels * bitDepth / 8, true);
        // block align (channel count * bytes per sample)
        view.setUint16(32, numberOfChannels * bitDepth / 8, true);
        // bits per sample
        view.setUint16(34, bitDepth, true);
        // data chunk identifier
        this.writeString(view, 36, 'data');
        // data chunk length
        view.setUint32(40, result.length * 2, true);

        // write the PCM samples
        this.floatTo16BitPCM(view, 44, result);

        return new Blob([view], { type: 'audio/wav' });
    },

    interleave(leftChannel, rightChannel) {
        const length = leftChannel.length + rightChannel.length;
        const result = new Float32Array(length);

        let inputIndex = 0;
        for (let i = 0; i < length;) {
            result[i++] = leftChannel[inputIndex];
            result[i++] = rightChannel[inputIndex];
            inputIndex++;
        }
        return result;
    },

    writeString(view, offset, string) {
        for (let i = 0; i < string.length; i++) {
            view.setUint8(offset + i, string.charCodeAt(i));
        }
    },

    floatTo16BitPCM(output, offset, input) {
        for (let i = 0; i < input.length; i++, offset += 2) {
            const s = Math.max(-1, Math.min(1, input[i]));
            output.setInt16(offset, s < 0 ? s * 0x8000 : s * 0x7FFF, true);
        }
    },

    // Download file helper
    downloadAudio(base64Data, filename, mimeType) {
        const binaryString = atob(base64Data);
        const bytes = new Uint8Array(binaryString.length);
        for (let i = 0; i < binaryString.length; i++) {
            bytes[i] = binaryString.charCodeAt(i);
        }

        const blob = new Blob([bytes], { type: mimeType });
        const url = URL.createObjectURL(blob);

        const a = document.createElement('a');
        a.href = url;
        a.download = filename;
        document.body.appendChild(a);
        a.click();

        setTimeout(() => {
            document.body.removeChild(a);
            URL.revokeObjectURL(url);
        }, 0);
    }
};
