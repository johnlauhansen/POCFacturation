using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using POC_Facturation.Domain;
using POC_Facturation.Domain.Repositories;

namespace POC_Facturation.Data.Repositories;

internal class DogRepository : IDogRepository
{
    private readonly InvoiceDbContext _context;

    public DogRepository(InvoiceDbContext context)
    {
        _context = context;
    }

    public async Task<DogDetail?> GetByIdAsync(int id)
    {
        return await _context.DogDetails.FindAsync(id);
    }

    public async Task<IEnumerable<DogDetail>> GetAllAsync()
    {
        return await _context.DogDetails.ToListAsync();
    }

    public async Task AddAsync(DogDetail dog)
    {
        await _context.DogDetails.AddAsync(dog);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(DogDetail dog)
    {
        _context.DogDetails.Update(dog);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var dog = await _context.DogDetails.FindAsync(id);
        if (dog != null)
        {
            _context.DogDetails.Remove(dog);
            await _context.SaveChangesAsync();
        }
    }
}
