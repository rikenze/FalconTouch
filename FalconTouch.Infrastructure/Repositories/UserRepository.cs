using FalconTouch.Domain.Entities;
using FalconTouch.Domain.Repositories;
using FalconTouch.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FalconTouch.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly FalconTouchDbContext _db;

        public UserRepository(FalconTouchDbContext db)
        {
            _db = db;
        }

        public async Task AddUserAsync(User user)
        {
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _db.Users.SingleOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User?> GetUserByIdAsync(int id)
        {
            return await _db.Users.FindAsync(id);
        }

        public async Task UpdateUserAsync(User user)
        {
            _db.Users.Update(user);
            await _db.SaveChangesAsync();
        }

        public async Task<bool> UserExistsAsync(string email)
        {
            return await _db.Users.AnyAsync(u => u.Email == email);
        }
    }
}
