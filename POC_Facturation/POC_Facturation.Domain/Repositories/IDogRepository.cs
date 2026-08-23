using System.Collections.Generic;
using System.Threading.Tasks;

namespace POC_Facturation.Domain.Repositories;

public interface IDogRepository
{
    Task<DogDetail?> GetByIdAsync(int id);
    Task<IEnumerable<DogDetail>> GetAllAsync();
    Task AddAsync(DogDetail dog);
    Task UpdateAsync(DogDetail dog);
    Task DeleteAsync(int id);
}
