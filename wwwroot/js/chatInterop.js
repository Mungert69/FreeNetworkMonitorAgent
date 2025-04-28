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

  recordingHandles: {},

// Check if recording is supported
checkRecordingSupport: async function() {
  if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
    return false;
  }
  
  try {
    const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
    stream.getTracks().forEach(track => track.stop());
    return true;
  } catch (err) {
    console.error("Recording not supported:", err);
    return false;
  }
},

startRecording: async function(sessionId) {
  console.log("startRecording called with sessionId:", sessionId);
  
  try {
    // Initialize recordingHandles if it doesn't exist
    if (!this.recordingHandles) {
      this.recordingHandles = {};
    }
    
    // Check if already recording
    if (this.recordingHandles[sessionId]) {
      console.log("Already recording this session");
      return false;
    }
    
    if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
      console.error("Media devices not supported");
      return false;
    }
    
    const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
    console.log("Got media stream");
    
    const options = { mimeType: 'audio/webm' };
    const mediaRecorder = new MediaRecorder(stream, options);
    const audioChunks = [];
    
    mediaRecorder.ondataavailable = function(event) {
      if (event.data.size > 0) {
        audioChunks.push(event.data);
        console.log("Data chunk added, size:", event.data.size);
      }
    };
    
    mediaRecorder.start(250);
    console.log("MediaRecorder started");
    
    // Store recording handle
    this.recordingHandles[sessionId] = {
      mediaRecorder: mediaRecorder,
      audioChunks: audioChunks,
      stream: stream
    };
    
    console.log("Recording started for session:", sessionId);
    console.log("Current recordingHandles:", Object.keys(this.recordingHandles));
    
    return true;
  } catch (error) {
    console.error("Error starting recording:", error);
    return false;
  }
},

stopRecording: function(sessionId) {
  console.log("stopRecording called with sessionId:", sessionId);
  console.log("this.recordingHandles:", this.recordingHandles);
  
  // If recordingHandles is empty but we know we're recording, recreate the object
  if (!this.recordingHandles || Object.keys(this.recordingHandles).length === 0) {
    console.warn("recordingHandles is empty, this may be due to a page refresh or navigation");
  }
  
  const recorderObj = this.recordingHandles ? this.recordingHandles[sessionId] : null;
  console.log("recorderObj:", recorderObj);
  
  if (!recorderObj || !recorderObj.mediaRecorder) {
    console.error("No valid recorder found for session:", sessionId);
    return null;
  }
  
  return new Promise((resolve) => {
    const handleAudioData = () => {
      try {
        if (!recorderObj.audioChunks || recorderObj.audioChunks.length === 0) {
          console.error("No audio chunks available");
          resolve(null);
          return;
        }
        
        const audioBlob = new Blob(recorderObj.audioChunks, { type: 'audio/webm' });
        console.log("Audio blob created, size:", audioBlob.size);
        
        const reader = new FileReader();
        reader.onloadend = function() {
          try {
            const base64data = reader.result.split(',')[1];
            console.log("Base64 conversion complete, length:", base64data.length);
            
            // Clean up
            if (recorderObj.stream) {
              recorderObj.stream.getTracks().forEach(track => track.stop());
            }
            
            // Make sure we use the right context for cleanup
            if (window.chatInterop.recordingHandles) {
              delete window.chatInterop.recordingHandles[sessionId];
            }
            
            resolve(base64data);
          } catch (err) {
            console.error("Error in reader.onloadend:", err);
            resolve(null);
          }
        };
        
        reader.onerror = function() {
          console.error("FileReader error");
          resolve(null);
        };
        
        reader.readAsDataURL(audioBlob);
      } catch (err) {
        console.error("Error handling audio data:", err);
        resolve(null);
      }
    };
    
    try {
      if (recorderObj.mediaRecorder.state !== "inactive") {
        recorderObj.mediaRecorder.onstop = handleAudioData;
        recorderObj.mediaRecorder.stop();
      } else {
        console.log("MediaRecorder already inactive");
        handleAudioData();
      }
    } catch (err) {
      console.error("Error stopping recorder:", err);
      resolve(null);
    }
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