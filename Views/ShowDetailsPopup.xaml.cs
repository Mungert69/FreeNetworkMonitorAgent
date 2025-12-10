using CommunityToolkit.Maui.Views;

namespace NetworkMonitorAgent.Views
{
    // Change from 'Popup' to 'Popup<bool>'
    public partial class StatusDetailsPopup : Popup<bool>
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
                // Pass the result directly to CloseAsync
                await CloseAsync(true);
            }
            catch { }
        }

        public async void OnCloseButtonClicked(object? sender, EventArgs e)
        {
            try
            {
                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
                // Pass the result directly to CloseAsync
                await CloseAsync(false);
            }
            catch { }
        }
    }
}   