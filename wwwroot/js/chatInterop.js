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


    startRecording: async function(sessionId) {  // Note: no arrow function
        try {
            const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
            const mediaRecorder = new MediaRecorder(stream, { mimeType: 'audio/webm' });
            const audioChunks = [];
            
            mediaRecorder.ondataavailable = (event) => {
                if (event.data.size > 0) {
                    audioChunks.push(event.data);
                }
            };
            
            mediaRecorder.start(100);
            
            // Now 'this' refers to chatInterop object
            self.recordingHandles[sessionId] = {
                mediaRecorder: mediaRecorder,
                audioChunks: audioChunks,
                stream: stream
            };
            
            return true;
        } catch (error) {
            console.error('Error starting recording:', error);
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