using System;
using System.Linq;
using System.Threading;
using System.Windows;
using POC_Facturation.Services;

namespace POC_Facturation.Services;

/// <summary>
/// Implémentation WPF de notification utilisateur non bloquante.
/// Utilise BeginInvoke sur le Dispatcher, rattache le dialogue à la fenêtre active
/// et intègre un mécanisme anti-flooding pour éviter d'empiler plusieurs boîtes de dialogue simultanées.
/// </summary>
public class WpfUserNotifier : IUserNotifier
{
    private static int _isDialogActive;

    public void NotifyWarning(string message, string title = "Avertissement")
    {
        DisplayAsync(message, title, MessageBoxImage.Warning);
    }

    public void NotifyError(string message, string title = "Erreur inattendue")
    {
        DisplayAsync(message, title, MessageBoxImage.Error);
    }

    private static void DisplayAsync(string message, string title, MessageBoxImage image)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher != null && !dispatcher.HasShutdownStarted && !dispatcher.HasShutdownFinished)
        {
            dispatcher.BeginInvoke(() => ShowModalDialog(message, title, image));
        }
        else
        {
            ShowModalDialog(message, title, image);
        }
    }

    private static void ShowModalDialog(string message, string title, MessageBoxImage image)
    {
        // Empêche l'accumulation de modales si plusieurs erreurs surviennent en rafale
        if (Interlocked.CompareExchange(ref _isDialogActive, 1, 0) != 0)
        {
            return;
        }

        try
        {
            // Rattachement de la modale à la fenêtre active pour éviter qu'elle ne passe en arrière-plan
            var activeWindow = Application.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)
                               ?? Application.Current?.MainWindow;

            if (activeWindow != null && activeWindow.IsLoaded)
            {
                MessageBox.Show(activeWindow, message, title, MessageBoxButton.OK, image);
            }
            else
            {
                MessageBox.Show(message, title, MessageBoxButton.OK, image);
            }
        }
        catch
        {
            // Protection silencieuse en cas d'erreur de rendu UI
        }
        finally
        {
            Interlocked.Exchange(ref _isDialogActive, 0);
        }
    }
}
