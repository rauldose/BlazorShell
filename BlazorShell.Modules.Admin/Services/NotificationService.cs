namespace BlazorShell.Modules.Admin.Services
{
    public enum NotificationLevel
    {
        Success,
        Error,
        Warning,
        Info
    }

    public class Notification
    {
        public string Message { get; set; } = string.Empty;
        public NotificationLevel Level { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public TimeSpan? Duration { get; set; }
    }

    public interface INotificationService
    {
        event EventHandler<Notification>? NotificationAdded;
        void ShowSuccess(string message, TimeSpan? duration = null);
        void ShowError(string message, TimeSpan? duration = null);
        void ShowWarning(string message, TimeSpan? duration = null);
        void ShowInfo(string message, TimeSpan? duration = null);
        void Show(string message, NotificationLevel level, TimeSpan? duration = null);
    }

    public class NotificationService : INotificationService
    {
        public event EventHandler<Notification>? NotificationAdded;

        public void ShowSuccess(string message, TimeSpan? duration = null)
        {
            Show(message, NotificationLevel.Success, duration);
        }

        public void ShowError(string message, TimeSpan? duration = null)
        {
            Show(message, NotificationLevel.Error, duration);
        }

        public void ShowWarning(string message, TimeSpan? duration = null)
        {
            Show(message, NotificationLevel.Warning, duration);
        }

        public void ShowInfo(string message, TimeSpan? duration = null)
        {
            Show(message, NotificationLevel.Info, duration);
        }

        public void Show(string message, NotificationLevel level, TimeSpan? duration = null)
        {
            var notification = new Notification
            {
                Message = message,
                Level = level,
                Duration = duration ?? TimeSpan.FromSeconds(5) // Default 5 seconds
            };

            NotificationAdded?.Invoke(this, notification);
        }
    }
}