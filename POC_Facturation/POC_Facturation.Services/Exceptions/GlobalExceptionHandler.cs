using System;
using System.Text;
using Microsoft.Extensions.Logging;

namespace POC_Facturation.Services;

/// <summary>
/// Gestionnaire centralisé des exceptions applicatives.
/// Journalise l'erreur via ILogger et informe l'utilisateur via IUserNotifier.
/// </summary>
public class GlobalExceptionHandler : IGlobalExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IUserNotifier _userNotifier;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IUserNotifier userNotifier)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _userNotifier = userNotifier ?? throw new ArgumentNullException(nameof(userNotifier));
    }

    public void HandleException(Exception? exception, string source, bool isFatal = false)
    {
        if (exception == null) return;

        var baseException = exception.GetBaseException();

        // 1. Journalisation prioritaire (avec résilience)
        try
        {
            if (isFatal)
            {
                _logger.LogCritical(exception, "[FATAL] Exception critique interceptée depuis {Source} : {Message}", source, exception.Message);
            }
            else
            {
                _logger.LogError(exception, "Exception non gérée interceptée depuis {Source} : {Message}", source, exception.Message);
            }
        }
        catch
        {
            // Protection si le sous-système de logging rencontre une défaillance
        }

        // 2. Notification utilisateur sécurisée
        try
        {
            string userMessage = FormatUserMessage(baseException, source, isFatal);
            string title = isFatal ? "Erreur critique de l'application" : "Erreur inattendue";

            _userNotifier.NotifyError(userMessage, title);
        }
        catch (Exception notifierEx)
        {
            try
            {
                _logger.LogWarning(notifierEx, "Impossible de notifier l'utilisateur de l'erreur via IUserNotifier.");
            }
            catch
            {
            }
        }
    }

    private static string FormatUserMessage(Exception baseException, string source, bool isFatal)
    {
        var sb = new StringBuilder();
        if (isFatal)
        {
            sb.AppendLine("Une erreur critique est survenue et l'application pourrait devoir s'arrêter.");
        }
        else
        {
            sb.AppendLine("Une erreur inattendue est survenue, mais l'application a pu continuer son exécution.");
        }

        sb.AppendLine();
        sb.Append("Détail : ").AppendLine(baseException.Message);
        sb.Append("Origine : ").AppendLine(source);
        sb.AppendLine();
        sb.AppendLine("Un rapport complet a été consigné dans le journal des logs.");
        sb.AppendLine("Veuillez consulter le fichier log ou contacter l'administrateur si le problème persiste.");

        return sb.ToString();
    }
}
