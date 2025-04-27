using Microsoft.JSInterop;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System;

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
        private IJSObjectReference? _currentRecorder;

        public AudioService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
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
        public async Task<bool> CheckAndRequestRecordingPermission()
        {
            try
            {
                await EnsureInitialized();
                return await _jsRuntime.InvokeAsync<bool>("chatInterop.requestRecordingPermission");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Permission check failed: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> IsRecordingSupported()
        {
            try
            {
                await EnsureInitialized();
                return await _jsRuntime.InvokeAsync<bool>("chatInterop.checkRecordingSupport");
            }
            catch
            {
                return false;
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

        public async Task StartRecording()
        {
            await EnsureInitialized();
            _currentRecorder = await _jsRuntime.InvokeAsync<IJSObjectReference>(
                "chatInterop.startRecording");
        }

        public async Task<byte[]> StopRecording()
        {
            await EnsureInitialized();
            if (_currentRecorder != null)
            {
                return await _jsRuntime.InvokeAsync<byte[]>(
                    "chatInterop.stopRecording",
                    _currentRecorder);
            }
            return Array.Empty<byte>();
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

        public async Task<string> TranscribeAudio(byte[] audioBlob)
        {
            await EnsureInitialized();
            return await _jsRuntime.InvokeAsync<string>(
                "chatInterop.transcribeAudio", audioBlob);
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