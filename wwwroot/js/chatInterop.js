// Audio handling functions
window.chatInterop = {
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


    // Audio recording
    startRecording: async () => {
        try {
            const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
            const mediaRecorder = new MediaRecorder(stream);
            const audioChunks = [];
            
            mediaRecorder.ondataavailable = (event) => {
                audioChunks.push(event.data);
            };
            
            mediaRecorder.onstop = async () => {
                const audioBlob = new Blob(audioChunks, { type: 'audio/wav' });
                const arrayBuffer = await audioBlob.arrayBuffer();
                DotNet.invokeMethodAsync('NetworkMonitorAgent', 'ReceiveAudioBlob', arrayBuffer);
            };
            
            mediaRecorder.start();
            return {
                mediaRecorder: mediaRecorder,
                stream: stream
            };
        } catch (error) {
            console.error('Error starting recording:', error);
            throw error;
        }
    },

    stopRecording: async (recorderObj) => {
        if (recorderObj && recorderObj.mediaRecorder) {
            recorderObj.mediaRecorder.stop();
            if (recorderObj.stream) {
                recorderObj.stream.getTracks().forEach(track => track.stop());
            }
        }
    },

    // File download
    downloadFile: function(filename, content, contentType) {
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
    
    scrollToBottom: function(element) {
        element.scrollTop = element.scrollHeight;
    }
};