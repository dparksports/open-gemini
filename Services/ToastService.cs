using System;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;

namespace OpenClaw.Windows.Services
{
    public class ToastService
    {
        public ToastService()
        {
            // Register for notifications 
            // Note: In packaged apps this is handled by the manifest, 
            // but ensuring the manager works is good practice.
            var manager = AppNotificationManager.Default;
            manager.Register();
        }

        public void ShowToast(string title, string message)
        {
            try 
            {
                var notification = new AppNotificationBuilder()
                    .AddText(title)
                    .AddText(message)
                    .BuildNotification();

                AppNotificationManager.Default.Show(notification);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to show toast: {ex.Message}");
            }
        }
    }
}
