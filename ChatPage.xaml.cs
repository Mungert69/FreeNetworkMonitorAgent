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

            if (this.Content is BlazorWebView bw)
            {
                bw.BlazorWebViewInitialized += (sender, args) =>
                {
#if ANDROID
            // Enable WebView2 features for Android
            var webView = args.WebView;
            var settings = webView.Settings;
            settings.JavaScriptEnabled = true;
            settings.MediaPlaybackRequiresUserGesture = false;
            settings.AllowFileAccess = true;
            settings.AllowContentAccess = true;
#endif
                };
            }
        }
    }
}