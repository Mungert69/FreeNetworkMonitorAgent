using System;
using Microsoft.Maui.Controls;

namespace NetworkMonitorAgent
{
    public partial class SetupGuidePage : ContentPage
    {
        public SetupGuidePage()
        {
            InitializeComponent();
        }

        private async void OnDownloadLinkClicked(object sender, EventArgs e)
        {
            await Browser.Default.OpenAsync("http://freenetworkmonitor.click/download");
        }
    }
}
