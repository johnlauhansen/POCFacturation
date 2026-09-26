namespace POC_Facturation.Services;

/// <summary>
/// Contrat d'abstraction pour notifier l'utilisateur lors d'avertissements ou d'erreurs,
/// permettant de découpler la couche Services de l'infrastructure UI (WPF).
/// </summary>
public interface IUserNotifier
{
    void NotifyWarning(string message, string title = "Avertissement");
    void NotifyError(string message, string title = "Erreur inattendue");
}
