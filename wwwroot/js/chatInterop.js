window.chatInterop = {
    recordingHandles: {}, // To track active recordings

    playAudio: (audioFile) => {
        const audio = new Audio(audioFile);
        audio.play().catch(e => console.error("Audio playback failed:", e));
    },

    pauseAudio: () => {
        const audios = document.getElementsByTagName('audio');
        for (let audio of audios) {
            audio.pause();
        }
    },

    playAudioWithCallback: (audioFile, dotnetRef) => {
        const audio = new Audio(audioFile);
        audio.play().catch(e => {
            console.error("Audio playback failed:", e);
            dotnetRef.invokeMethodAsync('OnAudioEnded');
        });
        audio.onended = () => {
            dotnetRef.invokeMethodAsync('OnAudioEnded');
            dotnetRef.dispose();
        };
    },

    checkRecordingSupport: async function() {
        return navigator.mediaDevices && 
               navigator.mediaDevices.getUserMedia && 
               window.MediaRecorder;
    },

    requestRecordingPermission: async function() {
        try {
            const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
            stream.getTracks().forEach(track => track.stop());
            return true;
        } catch (error) {
            console.error('Permission denied:', error);
            return false;
        }
    },

    startRecording: async function(sessionId) {
        try {
            if (!await this.checkRecordingSupport()) {
                throw new Error('Recording not supported');
            }

            const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
            const options = { mimeType: 'audio/webm' };
            const mediaRecorder = new MediaRecorder(stream, options);
            
            const audioChunks = [];
            mediaRecorder.ondataavailable = (event) => {
                if (event.data.size > 0) {
                    audioChunks.push(event.data);
                }
            };
            
            mediaRecorder.start(250); // Collect data every 250ms
            
            this.recordingHandles[sessionId] = {
                mediaRecorder,
                audioChunks,
                stream
            };
            
            return true;
        } catch (error) {
            console.error('Recording error:', error);
            throw error;
        }
    },
    stopRecording: async (sessionId) => {
        const recorderObj = self.recordingHandles[sessionId];
        if (!recorderObj) {
            throw new Error('No active recording found');
        }

        return new Promise((resolve) => {
            recorderObj.mediaRecorder.onstop = async () => {
                const audioBlob = new Blob(recorderObj.audioChunks, { type: 'audio/webm' });

                // Convert blob to array buffer
                const arrayBuffer = await new Response(audioBlob).arrayBuffer();

                // Clean up
                recorderObj.stream.getTracks().forEach(track => track.stop());
                delete this.recordingHandles[sessionId];

                // Convert to byte array
                const byteArray = new Uint8Array(arrayBuffer);
                resolve(Array.from(byteArray));
            };

            recorderObj.mediaRecorder.stop();
        });
    },
    // File download
    downloadFile: function (filename, content, contentType) {
        // Create a blob with the content
        const blob = new Blob([content], { type: contentType || 'application/octet-stream' });
        const url = URL.createObjectURL(blob);

        // Create a temporary anchor element
        const a = document.createElement('a');
        a.href = url;
        a.download = filename;
        document.body.appendChild(a);
        a.click();

        // Cleanup
        setTimeout(() => {
            document.body.removeChild(a);
            URL.revokeObjectURL(url);
        }, 100);
    },

    scrollToBottom: function (element) {
        element.scrollTop = element.scrollHeight;
    }
};