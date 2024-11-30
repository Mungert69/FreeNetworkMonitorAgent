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
        public event EventHandler<bool> ShowLoadingMessage;


        public ObservableCollection<TaskItem> Tasks { get; set; }

        //public ICommand ToggleServiceCommand { get; }
        public ICommand CloseAppCommand { get; }
        private bool _isPolling;
        public bool IsPolling
        {
            get => _isPolling;
            set => SetProperty(ref _isPolling, value);
        }



        private async Task SetServiceStartedAsync(bool value)
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

        public bool IsServiceStarted
        {
            get => _platformService.IsServiceStarted;
            set
            {
                if (_platformService.IsServiceStarted != value)
                {
                    // Fire and forget the async call
                    SetServiceStartedAsync(value).ConfigureAwait(false);
                }
            }
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

        public bool ShowTasks
        {
            get
            {
                if (_platformService.DisableAgentOnServiceShutdown) return _showTasks && _platformService.IsServiceStarted;
                return _platformService.IsServiceStarted;
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
                await Task.Run(async () => await _platformService.ChangeServiceState(state));
            }
            finally
            {
                try
                {
                    ShowLoadingMessage?.Invoke(this, false);
                }
                catch { }
                ShowToggle = true;
            }
        }



        public string ServiceMessage
        {
            get => _platformService.ServiceMessage;
        }

        public MainPageViewModel(NetConnectConfig netConfig, IPlatformService platformService, ILogger logger)
        {
            _netConfig = netConfig;
            _platformService = platformService;
            _logger = logger;
            CloseAppCommand = new Command(CloseApp);
            //ToggleServiceCommand = new Command(async () => await ToggleServiceAsync());
            _platformService.ServiceStateChanged += PlatformServiceStateChanged;
            _netConfig.AgentUserFlow.PropertyChanged += OnAgentUserFlowPropertyChanged;

        }

        public void SetupTasks()
        {
            Action wrappedAuthorizeAction = () =>
                           {
                               try
                               {
                                   if (!IsPolling)
                                   {
                                       IsPolling = true;
                                       AuthorizeAction?.Invoke();
                                   }
                               }
                               catch (Exception ex)
                               {
                                   _logger.LogError($"Error executing authorize action: {ex}");
                                   // Handle failure (e.g., reset polling status, notify user)
                               }
                           };
            Tasks = new ObservableCollection<TaskItem>
            {
                new TaskItem { TaskDescription = "Authorize Agent", IsCompleted = _netConfig.AgentUserFlow.IsAuthorized, TaskAction = new Command(wrappedAuthorizeAction) },
                new TaskItem { TaskDescription = "Login Free Network Monitor", IsCompleted = _netConfig.AgentUserFlow.IsLoggedInWebsite, TaskAction = new Command(LoginAction) },
                new TaskItem { TaskDescription = "Scan for Hosts", IsCompleted = _netConfig.AgentUserFlow.IsHostsAdded, TaskAction = new Command(AddHostsAction) }
            };
        }

        public void CloseApp()
        {
            //var activity = (Activity)Microsoft.Maui.Application.Current.Context;
            //activity.FinishAffinity();
        }

        private void PlatformServiceStateChanged(object? sender, EventArgs e)
        {
            try
            {
                OnPropertyChanged(nameof(IsServiceStarted));
                OnPropertyChanged(nameof(ServiceMessage));
                OnPropertyChanged(nameof(ShowTasks));
                OnPropertyChanged(nameof(ShowToggle));
            }
            catch (Exception ex)
            {
                _logger.LogError($" Error : handling service state change: {ex.Message}");
            }
        }
        private void OnAgentUserFlowPropertyChanged(object? sender, PropertyChangedEventArgs e)
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
                _logger.LogError($"Error updating task completion for {taskDescription}: {ex}");
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
            if (Equals(storage, value))
            {
                return false;
            }

            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }

    public class TaskItem : INotifyPropertyChanged
    {
        private bool _isCompleted;
        public string TaskDescription { get; set; } = "";
        public string ButtonText => IsCompleted ? $"{TaskDescription} (Completed)" : TaskDescription;
        public Color ButtonBackgroundColor
        {
            get
            {
                if (_isCompleted)
                {
                    if (App.Current?.RequestedTheme == AppTheme.Dark)
                    {
                        return ColorResource.GetResourceColor("Grey(950");
                    }
                    else
                    {
                        return Colors.White;
                    }
                }
                else
                {
                    return ColorResource.GetResourceColor("Warning");
                }

            }
        }
        public Color ButtonTextColor
        {
            get
            {
                if (_isCompleted)
                {
                    return ColorResource.GetResourceColor("Primary");
                }
                else { return Colors.White; }
            }
        }
        public bool IsCompleted
        {
            get => _isCompleted;
            set
            {
                _isCompleted = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ButtonText));
                OnPropertyChanged(nameof(ButtonBackgroundColor));
                OnPropertyChanged(nameof(ButtonTextColor));
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
