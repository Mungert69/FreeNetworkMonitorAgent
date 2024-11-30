using System.ComponentModel;
using System.Runtime.CompilerServices;
using NetworkMonitor.Objects;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using Microsoft.Extensions.Logging;
namespace NetworkMonitorAgent.ViewModels
{
    public class ProcessorStatesViewModel : BasePopupViewModel
    {
        private LocalProcessorStates _processorStates;
        public ICommand ShowPopupCommand { get; private set; }
        private ILogger _logger;
        private string _popupMessageType="";

        public ProcessorStatesViewModel(ILogger logger, LocalProcessorStates processorStates)
        {
            try
            {
                // _logger = MauiProgram.ServiceProvider.GetRequiredService<ILogger<ProcessorStatesViewModel>>();
                // _processorStates = MauiProgram.ServiceProvider.GetRequiredService<LocalProcessorStates>();
                _logger = logger; _processorStates=processorStates;
                _processorStates.PropertyChanged += OnProcessorStatesChanged;

                ShowPopupCommand = new Command<string>(ShowPopupWithMessage);
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Error initializing ProcessorStatesViewModel: {ex}");
                // Consider fallback logic or notifying the user of the error
            }


        }

        public bool IsRunning => _processorStates.IsRunning;
        public bool IsSetup => _processorStates.IsSetup;
        public ConnectState IsConnectState => _processorStates.IsConnectState;
        public bool IsRabbitConnected => _processorStates.IsRabbitConnected;
        public string SetupMessage => _processorStates.SetupMessage;
        public string RabbitSetupMessage => _processorStates.RabbitSetupMessage;

        public string RunningMessage => _processorStates.RunningMessage;
        public string ConnectRunningMessage => _processorStates.ConnectRunningMessage;

        private void OnProcessorStatesChanged(object? sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged(e.PropertyName);

            if (IsPopupVisible)
            {
                // Update PopupMessage based on the changed property
                UpdatePopupMessage(e.PropertyName);
            }
        }

        private void UpdatePopupMessage(string? propertyName)
        {
            // Logic to update PopupMessage based on propertyName
            switch (propertyName)
            {
                case nameof(RunningMessage):
                    if (_popupMessageType=="RunningMessage") PopupMessage = $"Running Message: {RunningMessage}";
                    break;
                case nameof(ConnectRunningMessage):
                    if (_popupMessageType=="ConnectRunningMessage") PopupMessage = $"Monitor Message: {ConnectRunningMessage}";

                    break;
                case nameof(SetupMessage):
                    if (_popupMessageType=="SetupMessage") PopupMessage = $"Running Message: {RunningMessage}";
                   PopupMessage = $"Setup Message: {SetupMessage}";
                    break;
                case nameof(RabbitSetupMessage):
                     if (_popupMessageType=="RabbitSetupMessage") PopupMessage = $"Rabbit Setup Message: {RabbitSetupMessage}";
                    break;
                    // Add additional cases as needed
            }
        }


        private  void ShowPopupWithMessage(string messageType)
        {
           _popupMessageType=messageType;
            switch (messageType)
            {
                case "RunningMessage":
                    PopupMessage = $"Running Message: {RunningMessage}";
                    break;
                case "ConnectRunningMessage":
                    PopupMessage = $"Monitor Message: {ConnectRunningMessage}";

                    break;
                case "SetupMessage":
                    PopupMessage = $"Setup Message: {SetupMessage}";
                    break;
                case "RabbitSetupMessage":
                    PopupMessage = $"Rabbit Setup Message: {RabbitSetupMessage}";
                    break;
                    // Add additional cases as needed
            }
            IsPopupVisible = true;
        }

    }

}