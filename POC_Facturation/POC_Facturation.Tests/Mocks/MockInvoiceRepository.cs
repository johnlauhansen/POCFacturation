using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using POC_Facturation.Domain;
using POC_Facturation.Domain.Repositories;

namespace POC_Facturation.Tests.Mocks;

public class MockInvoiceRepository : IInvoiceRepository
{
    public List<Invoice> Invoices { get; set; } = new();
    public bool AddCalled { get; private set; }
    public bool UpdateCalled { get; private set; }

    public Task<Invoice?> GetByIdAsync(int id)
    {
        return Task.FromResult(Invoices.FirstOrDefault(i => i.Id == id));
    }

    public Task<IEnumerable<Invoice>> GetAllAsync()
    {
        return Task.FromResult<IEnumerable<Invoice>>(Invoices);
    }

    public Task AddAsync(Invoice invoice)
    {
        AddCalled = true;
        if (invoice.Id == 0)
        {
            invoice.Id = Invoices.Count + 1;
        }
        Invoices.Add(invoice);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Invoice invoice)
    {
        UpdateCalled = true;
        var existing = Invoices.FirstOrDefault(i => i.Id == invoice.Id);
        if (existing != null)
        {
            Invoices.Remove(existing);
            Invoices.Add(invoice);
        }
        return Task.CompletedTask;
    }

    public Task DeleteAsync(int id)
    {
        var existing = Invoices.FirstOrDefault(i => i.Id == id);
        if (existing != null)
        {
            Invoices.Remove(existing);
        }
        return Task.CompletedTask;
    }

    public Task<Invoice?> GetLastInvoiceAsync()
    {
        return Task.FromResult(Invoices.OrderByDescending(i => i.Id).FirstOrDefault());
    }
}
