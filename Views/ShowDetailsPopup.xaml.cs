using CommunityToolkit.Maui.Views;

namespace NetworkMonitorAgent.Views
{
    public partial class StatusDetailsPopup : Popup
    {
        public StatusDetailsPopup()
        {
            InitializeComponent();
        }
        public async void OnDetailsButtonClicked(object? sender, EventArgs e)
        {
            try
            {
                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
                await CloseAsync(true, cts.Token);
            }
            catch { }

        }

        public async void OnCloseButtonClicked(object? sender, EventArgs e)
        {
            try
            {
                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
                await CloseAsync(false, cts.Token);
            }
            catch { }

        }

        // Additional methods or event handlers
    }
}
