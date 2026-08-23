using System.Collections.Generic;
using System.Threading.Tasks;

namespace POC_Facturation.Domain.Repositories;

public interface IInvoiceRepository
{
    Task<Invoice?> GetByIdAsync(int id);
    Task<IEnumerable<Invoice>> GetAllAsync();
    Task AddAsync(Invoice invoice);
    Task UpdateAsync(Invoice invoice);
    Task DeleteAsync(int id);
    Task<Invoice?> GetLastInvoiceAsync();
}
