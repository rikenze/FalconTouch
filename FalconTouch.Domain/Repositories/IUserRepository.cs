using FalconTouch.Domain.Entities;
namespace FalconTouch.Domain.Repositories
{
    public interface IUserRepository
    {
        Task AddUserAsync(User user);
        Task<User?> GetUserByEmailAsync(string email);
        Task<User?> GetUserByIdAsync(int id);
        Task UpdateUserAsync(User user);
        Task<bool> UserExistsAsync(string email);
    }
}
