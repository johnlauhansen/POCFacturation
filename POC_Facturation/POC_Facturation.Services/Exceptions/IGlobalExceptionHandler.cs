using System;

namespace POC_Facturation.Services;

/// <summary>
/// Contrat pour la gestion centralisée et le traitement des exceptions de l'application.
/// </summary>
public interface IGlobalExceptionHandler
{
    void HandleException(Exception? exception, string source, bool isFatal = false);
}
