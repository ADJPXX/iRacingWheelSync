using System.Diagnostics;

namespace iRacingWheelSync;

public static class NotificationService
{
    public static void Send(string msg)
    {
        var psCommand = "[Windows.UI.Notifications.ToastNotificationManager, Windows.UI, ContentType = WindowsRuntime] | Out-Null; " +
                        "$template = [Windows.UI.Notifications.ToastNotificationManager]::GetTemplateContent([Windows.UI.Notifications.ToastTemplateType]::ToastText02); " +
                        $"$template.GetElementsByTagName('text')[0].AppendChild($template.CreateTextNode('{msg}')) | Out-Null; " +
                        "$template.GetElementsByTagName('text')[1].AppendChild($template.CreateTextNode('O arquivo joyCalib.yaml foi sincronizado com o novo GUID!')) | Out-Null; " +
                        "$toast = [Windows.UI.Notifications.ToastNotification]::new($template); " +
                        "$notifier = [Windows.UI.Notifications.ToastNotificationManager]::CreateToastNotifier('iRacingWheelSync'); " +
                        "$notifier.Show($toast);";

        Process.Start(new ProcessStartInfo
        {
            FileName = "powershell",
            Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{psCommand}\"",
            UseShellExecute = false,
            CreateNoWindow = true
        });
    }
}