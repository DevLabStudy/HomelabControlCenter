using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace HomelabControlCenter;

public partial class App : Application
{
    public App()
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            LogCrash(e.ExceptionObject as Exception);
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        LogCrash(e.Exception);
        e.Handled = true;
        Shutdown();
    }

    private static void LogCrash(Exception? ex)
    {
        try
        {
            string dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "HomelabControlCenter");
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, "crash.log");
            File.AppendAllText(path, $"[{DateTime.Now}] {ex}\n\n");
            MessageBox.Show($"Aplikacja napotkała błąd:\n\n{ex?.Message}\n\nSzczegóły: {path}",
                "HomelabControlCenter", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch { }
    }
}