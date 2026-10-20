namespace SchoolNewspaperBlazorApp.Service
{
    public enum NotificationType { Success, Error, Info }

    public class NotificationService
    {
        public event Action<string, NotificationType>? OnShow;

        public void Show(string message, NotificationType type = NotificationType.Success)
            => OnShow?.Invoke(message, type);
    }
}
