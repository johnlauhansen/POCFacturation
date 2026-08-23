using System.Threading.Tasks;

namespace POC_Facturation.Domain.Services;

public interface IInvoiceService
{
    /// <summary>
    /// Valide officiellement une facture en lui attribuant son numéro de séquence
    /// et en calculant sa signature cryptographique (chaînée à la précédente).
    /// Une fois validée, la facture devient immuable.
    /// </summary>
    Task ValidateAndSignInvoiceAsync(Invoice invoice);

    /// <summary>
    /// Crée un avoir (crédit) associé à une facture déjà validée afin de l'annuler
    /// ou de la rectifier légalement.
    /// </summary>
    Task<Invoice> CreateCreditNoteAsync(int originalInvoiceId);
}
