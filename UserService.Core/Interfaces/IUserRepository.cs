using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UserService.Core.Entities;

namespace UserService.Core.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(Guid id);
        Task<List<User>> GetAllAsync();
        Task<User> AddAsync(User user);       // <-- return the user
        Task<User?> UpdateAsync(User user);
        Task<bool> DeleteAsync(Guid id);
        Task SaveChangesAsync();
    }
}
