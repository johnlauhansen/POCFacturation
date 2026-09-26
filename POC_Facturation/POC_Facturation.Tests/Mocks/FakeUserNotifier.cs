using POC_Facturation.Services;

namespace POC_Facturation.Tests.Mocks;

/// <summary>
/// Bouchon de test pour IUserNotifier permettant de vérifier les notifications reçues sans UI.
/// </summary>
public class FakeUserNotifier : IUserNotifier
{
    public string? LastMessage { get; private set; }
    public string? LastTitle { get; private set; }
    public int NotificationCount { get; private set; }

    public void NotifyWarning(string message, string title = "Avertissement")
    {
        LastMessage = message;
        LastTitle = title;
        NotificationCount++;
    }

    public void NotifyError(string message, string title = "Erreur inattendue")
    {
        LastMessage = message;
        LastTitle = title;
        NotificationCount++;
    }
}
