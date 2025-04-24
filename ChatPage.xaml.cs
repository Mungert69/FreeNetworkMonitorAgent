using Microsoft.AspNetCore.Components.WebView.Maui;
using Microsoft.Extensions.Logging;

namespace NetworkMonitorAgent
{
    public partial class ChatPage : ContentPage
    {
        private readonly ILogger<ChatPage> _logger;

        public ChatPage(ILogger<ChatPage> logger)
        {
            _logger = logger;
            InitializeComponent();

            // Enhanced debugging
            this.NavigatedTo += (sender, e) => {
                _logger.LogInformation("ChatPage NavigatedTo event fired");

                if (this.Content is BlazorWebView bw)
                {
                    _logger.LogInformation("BlazorWebView instance found");

                    bw.BlazorWebViewInitializing += (s, args) =>
                        _logger.LogInformation("BlazorWebView initializing");

                    bw.BlazorWebViewInitialized += (s, args) =>
                    {
                        _logger.LogInformation("BlazorWebView initialized");
                        bw.Dispatcher.DispatchAsync(() =>
                            _logger.LogInformation("BlazorWebView dispatcher ready"));
                    };

                    bw.Loaded += (s, args) =>
                        _logger.LogInformation("BlazorWebView Loaded event fired");
                }
                else
                {
                    _logger.LogError("Content is not a BlazorWebView");
                }
            };
        }
    }
}