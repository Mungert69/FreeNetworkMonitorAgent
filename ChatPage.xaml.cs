
using Microsoft.AspNetCore.Components.WebView.Maui;
using Microsoft.Extensions.Logging;
using NetworkMonitor.Maui.Services;
using NetworkMonitor.Maui.ViewModels;
using NetworkMonitorAgent.Views;

namespace NetworkMonitorAgent
{
    public partial class ChatPage : ContentPage
    {
        private readonly ILogger<ChatPage> _logger;
        private readonly IPlatformService _platformService;

        public ChatPage(ILogger<ChatPage> logger, IPlatformService platformService)
        {
            _logger = logger;
            InitializeComponent();
            _platformService = platformService;


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
        protected override void OnAppearing()
        {
            base.OnAppearing();
            // Update _isAgentEnabled when the page appears
            UpdateVisibility();


        }

        public void UpdateVisibility()
        {
            try
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    ChatView.IsVisible = _platformService.IsServiceStarted;
                    AgentDisabledMessage.IsVisible = !_platformService.IsServiceStarted;
                });
            }
            catch (Exception ex)
            {
                if (_logger != null) _logger.LogError($" Error : in UpdateVisibility on ScanPage. Error was: {ex.Message}");
            }

        }
        private async void OnGoHomeClicked(object sender, EventArgs e)
        {
            try
            {
                await Shell.Current.GoToAsync("//Home");
            }
            catch (Exception ex)
            {
                if (_logger != null) _logger.LogError($" Error : in OnGoHomeClicked on LogsPage. Error was: {ex.Message}");
            }
        }

    }
}