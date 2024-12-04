using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using NetworkMonitor.Objects;
using NetworkMonitor.Connection;
using System.Windows.Input;
using NetworkMonitorAgent.Services;
using Microsoft.Extensions.Logging;

namespace NetworkMonitorAgent.ViewModels
{
    public class MainPageViewModel : INotifyPropertyChanged
    {
        private NetConnectConfig _netConfig;
        private IPlatformService _platformService;

        private ILogger _logger;
        public Action? AuthorizeAction;
        public Action? LoginAction;
        public Action? AddHostsAction;
        public ICommand ToggleServiceCommand { get; }
        public event EventHandler<bool> ShowLoadingMessage;


        public ObservableCollection<TaskItem> Tasks { get; set; }

        private bool _isPolling;
        public bool IsPolling
        {
            get => _isPolling;
            set => SetProperty(ref _isPolling, value);
        }

        private bool _showToggle = true;
        public bool ShowToggle
        {
            get => _showToggle;
            set
            {
                SetProperty(ref _showToggle, value);
                SetProperty(ref _showTasks, value);

            }
        }
        private bool _showTasks = true;


        public MainPageViewModel(NetConnectConfig netConfig, IPlatformService platformService, ILogger logger)
        {
            _netConfig = netConfig;
            _platformService = platformService;
            _logger = logger;
            if (_platformService != null)
            {
                _platformService.ServiceStateChanged += PlatformServiceStateChanged;
            }
            else
            {
                _logger.LogError("_platformService is null in MainPageViewModel constructor.");
            }

            if (_netConfig?.AgentUserFlow != null)
            {
                _netConfig.AgentUserFlow.PropertyChanged += OnAgentUserFlowPropertyChanged;
            }
            else
            {
                _logger.LogError("_netConfig.AgentUserFlow is null in MainPageViewModel constructor.");
            }
            ToggleServiceCommand = new Command<bool>(async (value) => await SetServiceStartedAsync(value));

        }




        public async Task SetServiceStartedAsync(bool value)
        {
            try
            {
                await ChangeServiceAsync(value);

                // Update the ShowToggle property based on the service state
                if (_platformService.IsServiceStarted && !value && _platformService.DisableAgentOnServiceShutdown)
                {
                    ShowToggle = false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error changing service state: {ex.Message}");
            }
        }



        public bool ShowTasks
        {
            get
            {
                if (_platformService?.DisableAgentOnServiceShutdown == true) return _showTasks && _platformService.IsServiceStarted;
                return _platformService?.IsServiceStarted ?? false;
            }
            set => SetProperty(ref _showTasks, value);
        }



        private async Task ChangeServiceAsync(bool state)
        {
            try
            {
                ShowToggle = false;
                ShowLoadingMessage?.Invoke(this, true);
                await Task.Delay(200); // A short delay, adjust as necessary
                await _platformService.ChangeServiceState(state);
            }
            catch (Exception e)
            {
                _logger.LogError($"Error in ChangeServiceAsync : {e.Message}");
            }
            finally
            {
                try
                {

                    ShowLoadingMessage?.Invoke(this, false);
                    ShowToggle = true;

                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error in ChangeServiceAsync : {ex.Message}");
                }

            }
        }



        public string ServiceMessage => _platformService?.ServiceMessage ?? "No Service Message";



        public void SetupTasks()
        {
            try
            {
                Action wrappedAuthorizeAction = () =>
                         {
                             try
                             {
                                 if (!IsPolling)
                                 {
                                     IsPolling = true;
                                     if (AuthorizeAction != null)
                                     {
                                         AuthorizeAction.Invoke();
                                     }
                                     else
                                     {
                                         _logger.LogWarning("AuthorizeAction is null.");
                                     }

                                 }

                             }
                             catch (Exception ex)
                             {
                                 _logger.LogError($"Error executing authorize action: {ex}");
                                 // Handle failure (e.g., reset polling status, notify user)
                             }
                         };
                MainThread.BeginInvokeOnMainThread(() =>
                    {
                        Tasks = new ObservableCollection<TaskItem>
 {
                new TaskItem { TaskDescription = "Authorize Agent", IsCompleted = _netConfig.AgentUserFlow.IsAuthorized, TaskAction = new Command(wrappedAuthorizeAction) },
                new TaskItem { TaskDescription = "Login Free Network Monitor", IsCompleted = _netConfig.AgentUserFlow.IsLoggedInWebsite, TaskAction = new Command(LoginAction) },
                new TaskItem { TaskDescription = "Scan for Hosts", IsCompleted = _netConfig.AgentUserFlow.IsHostsAdded, TaskAction = new Command(AddHostsAction) }
 };
                    });
            }
            catch (Exception e)
            {
                _logger.LogError($"Error in SetupTasks : {e.Message}");

            }

        }


        private void PlatformServiceStateChanged(object? sender, EventArgs e)
        {

            try
            {
                MainThread.BeginInvokeOnMainThread(() =>
                    {
                        OnPropertyChanged(nameof(ServiceMessage));
                        OnPropertyChanged(nameof(ShowTasks));
                        OnPropertyChanged(nameof(ShowToggle));
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError($" Error : handling service state change: {ex.Message}");
            }
        }
        private void OnAgentUserFlowPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            try
            {
                MainThread.BeginInvokeOnMainThread(() =>
                    {
                        switch (e.PropertyName)
                        {
                            case nameof(AgentUserFlow.IsAuthorized):
                                UpdateTaskCompletion("Authorize Agent", _netConfig.AgentUserFlow.IsAuthorized);
                                break;
                            case nameof(AgentUserFlow.IsLoggedInWebsite):
                                UpdateTaskCompletion("Login Free Network Monitor", _netConfig.AgentUserFlow.IsLoggedInWebsite);
                                break;
                            case nameof(AgentUserFlow.IsHostsAdded):
                                UpdateTaskCompletion("Scan for Hosts", _netConfig.AgentUserFlow.IsHostsAdded);
                                break;
                        }
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in OnAgentUserFlowPropertyChanged : {ex.Message}");

            }


        }

        public void UpdateTaskCompletion(string taskDescription, bool isCompleted)
        {
            try
            {
                var task = Tasks.FirstOrDefault(t => t.TaskDescription == taskDescription);
                if (task != null)
                {
                    task.IsCompleted = isCompleted;
                    OnPropertyChanged(nameof(Tasks)); // Notify the UI of changes
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating task completion for {taskDescription}: {ex.Message}");
                // Handle or log the error
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));


        }

        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
        {
            try
            {
                if (Equals(storage, value))
                {
                    return false;
                }

                storage = value;
                OnPropertyChanged(propertyName);
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError($"Error in SetProperty: {e.Message}");
                return false;
            }

        }
    }

    public class TaskItem : INotifyPropertyChanged
    {
        private bool _isCompleted;
        public string TaskDescription { get; set; } = "";
        public string ButtonText => IsCompleted ? $"{TaskDescription ?? "Task"} (Completed)" : TaskDescription ?? "Task";
        public Color ButtonBackgroundColor
        {
            get
            {
                Color color = Colors.White;
                try
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {

                        if (_isCompleted)
                        {
                            if (App.Current?.RequestedTheme == AppTheme.Dark)
                            {
                                color= ColorResource.GetResourceColor("Grey950");
                            }
                            else
                            {
                                color= Colors.White;
                            }
                        }
                        else
                        {
                            color= ColorResource.GetResourceColor("Warning");
                        }
                    });
                    return color;
                }
                catch (Exception e)
                {

                    return color;
                }

            }
        }
        public Color ButtonTextColor
        {
            get
            {
                try
                {
                    if (_isCompleted)
                    {
                        return ColorResource.GetResourceColor("Primary");
                    }
                    else { return Colors.White; }
                }
                catch (Exception e)
                {

                    return Colors.White;
                }

            }
        }
        public bool IsCompleted
        {
            get => _isCompleted;
            set
            {
                try
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        _isCompleted = value;
                        OnPropertyChanged();
                        OnPropertyChanged(nameof(ButtonText));
                        OnPropertyChanged(nameof(ButtonBackgroundColor));
                        OnPropertyChanged(nameof(ButtonTextColor));
                    });
                }
                catch (Exception e)
                {
                }

            }
        }
        public ICommand TaskAction { get; set; }
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));


        }

        protected void SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
        {

            if (Equals(storage, value))
            {
                return;
            }

            storage = value;
            OnPropertyChanged(propertyName);


        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
