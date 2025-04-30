using System;
using Microsoft.Maui.Controls;
using NetworkMonitor.Connection;

namespace NetworkMonitorAgent
{
    public partial class SetupGuidePage : ContentPage
    {
        private bool _isChatMode;
        public SetupGuidePage(NetConnectConfig netConfig)
        {
            InitializeComponent();

            _isChatMode=netConfig.IsChatMode;
            if (_isChatMode) ShowAlternateSteps(); else ShowDefaultSteps(); // or ShowAlternateSteps();
        }

        private async void OnDownloadLinkClicked(object sender, EventArgs e)
        {
            await Browser.Default.OpenAsync("http://freenetworkmonitor.click/download");
        }

        public void ShowDefaultSteps()
        {
            DefaultSteps.IsVisible = true;
            AlternateSteps.IsVisible = false;
        }

        public void ShowAlternateSteps()
        {
            DefaultSteps.IsVisible = false;
            AlternateSteps.IsVisible = true;
        }
    }
}