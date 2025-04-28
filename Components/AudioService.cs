using Microsoft.JSInterop;
using System;
using System.Net.Http;
using System.Net.Http.Headers; 
using NAudio.Wave;
using NetworkMonitor.Connection;
using System.IO;

namespace NetworkMonitorAgent
{
    public class AudioService : IAsyncDisposable
    {
        private readonly IJSRuntime _jsRuntime;
        private IJSObjectReference? _jsModule;
        private bool _isInitialized = false;
        private readonly Queue<string> _audioQueue = new Queue<string>();
        private bool _isPlaying = false;
        private CancellationTokenSource? _playbackCts;
        private readonly object _lock = new object();
        private string _apiUrl;
          private readonly HttpClient _httpClient;


        public AudioService(IJSRuntime jsRuntime, NetConnectConfig netConfig)
        {
            _jsRuntime = jsRuntime;
            _apiUrl= netConfig.TranscribeAudioUrl;
             _httpClient = new HttpClient()
        {
            Timeout = TimeSpan.FromSeconds(30) // Set appropriate timeout
        };
        }

        private async Task EnsureInitialized()
        {
            if (!_isInitialized)
            {
                _jsModule = await _jsRuntime.InvokeAsync<IJSObjectReference>(
                    "import", "./js/chatInterop.js");
                _isInitialized = true;
            }
        }

        public async Task PlayAudioSequentially(string audioFile)
        {
            await EnsureInitialized();

            lock (_lock)
            {
                _audioQueue.Enqueue(audioFile);
                if (!_isPlaying)
                {
                    _ = ProcessQueueAsync(); // Fire and forget
                }
            }
        }

        private async Task ProcessQueueAsync()
        {
            lock (_lock)
            {
                if (_isPlaying) return;
                _isPlaying = true;
                _playbackCts = new CancellationTokenSource();
            }

            try
            {
                while (true)
                {
                    string nextAudio;
                    lock (_lock)
                    {
                        if (_audioQueue.Count == 0 || _playbackCts?.IsCancellationRequested == true)
                        {
                            break;
                        }
                        nextAudio = _audioQueue.Dequeue();
                    }

                    try
                    {
                        // Create a promise that completes when audio finishes
                        var tcs = new TaskCompletionSource<bool>();
                        var dotnetRef = DotNetObjectReference.Create(new AudioCallbackHelper(tcs));

                        await _jsRuntime.InvokeVoidAsync(
                            "chatInterop.playAudioWithCallback",
                            nextAudio,
                            dotnetRef);

                        await tcs.Task.WaitAsync(_playbackCts.Token);
                    }
                    catch (TaskCanceledException)
                    {
                        // Playback was cancelled
                        break;
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"Error playing audio: {ex}");
                    }
                }
            }
            finally
            {
                lock (_lock)
                {
                    _isPlaying = false;
                    _playbackCts?.Dispose();
                    _playbackCts = null;
                }
            }
        }

        public async Task PauseAudio()
        {
            await EnsureInitialized();
            lock (_lock)
            {
                _playbackCts?.Cancel();
            }
            await _jsRuntime.InvokeVoidAsync("chatInterop.pauseAudio");
        }

        public async Task ClearQueue()
        {
            await EnsureInitialized();
            lock (_lock)
            {
                _audioQueue.Clear();
                _playbackCts?.Cancel();
            }
            await _jsRuntime.InvokeVoidAsync("chatInterop.pauseAudio");
        }

        public async Task<bool> StartRecording(string recordingSessionId)
        {
            await EnsureInitialized();
            return await _jsRuntime.InvokeAsync<bool>(
                "chatInterop.startRecording", recordingSessionId);
        }

        public async Task<byte[]> StopRecording(string recordingSessionId)
{
    try
    {
        await EnsureInitialized();
        
        string audioBase64 = await _jsRuntime.InvokeAsync<string>(
            "chatInterop.stopRecording", 
            recordingSessionId);

        if (string.IsNullOrEmpty(audioBase64))
        {
            Console.WriteLine("Received empty audio data");
            return Array.Empty<byte>();
        }

        // Convert base64 to byte array (still in webm format)
        byte[] webmBytes = Convert.FromBase64String(audioBase64);
        
        // Convert to WAV
        byte[] wavBytes = await ConvertWebmToWav(webmBytes);
        
        return wavBytes;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error stopping recording: {ex.Message}");
        return Array.Empty<byte>();
    }
}

public async Task<byte[]> ConvertWebmToWav(byte[] webmAudio)
{
    // 1. Write WebM bytes to a temporary file
    string tempWebmPath = Path.GetTempFileName();
    await File.WriteAllBytesAsync(tempWebmPath, webmAudio);

    try
    {
        using (var wavStream = new MemoryStream())
        {
            // 2. Read from temp file using MediaFoundationReader
            using (var reader = new MediaFoundationReader(tempWebmPath))
            {
                // 3. Set target format (16kHz, 16-bit, mono)
                WaveFormat targetFormat = new WaveFormat(16000, 16, 1);
                
                // 4. Resample if needed
                using (var resampler = new MediaFoundationResampler(reader, targetFormat))
                {
                    resampler.ResamplerQuality = 60;
                    
                    // 5. Write to WAV format
                    WaveFileWriter.WriteWavFileToStream(wavStream, resampler);
                }
            }
            return wavStream.ToArray();
        }
    }
    finally
    {
        // 6. Clean up temp file
        File.Delete(tempWebmPath);
    }
}
     public async Task<string> TranscribeAudio(byte[] audioBlob)
{
    try
    {
        // Convert to WAV if not already
        if (!IsWavFormat(audioBlob))
        {
            audioBlob = await ConvertWebmToWav(audioBlob);
        }

        using var content = new MultipartFormDataContent();
        using var audioContent = new ByteArrayContent(audioBlob);
        
        audioContent.Headers.ContentType = MediaTypeHeaderValue.Parse("audio/wav");
        content.Add(audioContent, "file", "recording.wav");

        var response = await _httpClient.PostAsync(_apiUrl, content);
        response.EnsureSuccessStatusCode();
        
        return await response.Content.ReadAsStringAsync();
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Transcription failed: {ex.Message}");
        return string.Empty;
    }
}

private bool IsWavFormat(byte[] audioData)
{
    // Check for WAV header "RIFF" signature
    return audioData.Length > 12 && 
           System.Text.Encoding.ASCII.GetString(audioData, 0, 4) == "RIFF" &&
           System.Text.Encoding.ASCII.GetString(audioData, 8, 4) == "WAVE";
}
        public async ValueTask DisposeAsync()
        {
            try
            {
                await ClearQueue();
                if (_jsModule is not null)
                {
                    await _jsModule.DisposeAsync();
                }
                _playbackCts?.Dispose();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error disposing AudioService: {ex}");
            }
        }



        private class AudioCallbackHelper
        {
            private readonly TaskCompletionSource<bool> _tcs;

            public AudioCallbackHelper(TaskCompletionSource<bool> tcs)
            {
                _tcs = tcs;
            }

            [JSInvokable]
            public void OnAudioEnded()
            {
                _tcs.TrySetResult(true);
            }
        }
    }
}