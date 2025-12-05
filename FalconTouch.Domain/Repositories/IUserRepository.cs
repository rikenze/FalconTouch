using FalconTouch.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FalconTouch.Domain.Repositories
{
    public interface IUserRepository
    {
        Task AddUserAsync(User user);
        Task<User?> GetUserByEmailAsync(string email);
        Task<bool> UserExistsAsync(string email);
    }
}
