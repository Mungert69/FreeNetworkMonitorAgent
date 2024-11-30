using CommunityToolkit.Maui.Views;

namespace NetworkMonitorAgent
{
    public partial class StatusDetailsPopup : Popup
    {
        public StatusDetailsPopup()
        {
            InitializeComponent();
            // Additional initialization

        }
        public async void OnDetailsButtonClicked(object? sender, EventArgs e)
        {
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            await CloseAsync(true, cts.Token);
        }

        public async void OnCloseButtonClicked(object? sender, EventArgs e)
        {
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            await CloseAsync(false, cts.Token);
        }

        // Additional methods or event handlers
    }
}
