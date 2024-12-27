using MetroLog.Maui;
using Microsoft.Extensions.Logging;

namespace NetworkMonitorAgent;
public partial class LogsPage : ContentPage
{
    private ILogger _logger;
    private LogController _logController;
    public LogsPage()
    {
        try
        {
            InitializeComponent();
            _logController = new LogController();
            BindingContext = _logController;
            _logger = MauiProgram.ServiceProvider.GetRequiredService<ILogger<ConfigPage>>();
        }
        catch (Exception ex)
        {
            if (_logger != null) _logger.LogError($" Error : Unable to load LogsPage. Error was: {ex.Message}");
        }
    }
    private async void ViewLogsButton_Clicked(object sender, EventArgs e)
    {
        try
        {

            _logController?.GoToLogsPageCommand.Execute(null);
        }
        catch (Exception ex)
        {
            // Handle any exceptions
            await DisplayAlert("Error", $"Unable to open logs page . Error was : {ex.Message}", "OK");
            _logger.LogError($" Error : Unable to open logs page . Error was : {ex.ToString()}");
        }
    }
}
